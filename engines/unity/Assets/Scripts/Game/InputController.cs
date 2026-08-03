using System;
using System.Collections.Generic;
using UnityEngine;

public class InputController : MonoBehaviour
{
    /// <summary>
    /// Max note-to-note span (seconds) for one FingerDown hit cluster
    /// (true or pseudo simultaneous press). Applies to Click / CDrag head / unheld Hold.
    /// Candidates are re-clustered each Down by <c>effectiveNoteTime</c> span ≤ this gap
    /// (no adjacent-gap chain expansion). Soft fallthrough across clusters remains.
    /// In-cluster rank: see <see cref="OrderHitCandidatesByNoteTimeClusters"/>.
    /// Flick stays list-order bind; Drag settles all colliding eligible notes per input.
    /// </summary>
    public const float NoteClusterGapSeconds = 0.015f;

    /// <summary>
    /// After a non-Miss Drag is accepted on the same FingerDown, block Click / CDrag head /
    /// Hold / Flick whose <c>effectiveNoteTime</c> is more than this many seconds later.
    /// Earlier or within-window selects stay eligible.
    /// Wider than <see cref="NoteClusterGapSeconds"/> (30ms vs 15ms) so short Drag+tap
    /// stacks co-clear on one Down without widening note-time clusters.
    /// Replaces legacy collidedDrag + Page.Duration/8.
    /// </summary>
    public const float DragCoHitWindowSeconds = 0.030f;

    /// <summary>
    /// Clear FX budget for stacked-drag settle; aliases
    /// <see cref="EffectController.MaxClearFxPerFrame"/> (also covers armed/Auto Clear).
    /// </summary>
    public const int DragBatchMaxClearFx = EffectController.MaxClearFxPerFrame;

    /// <summary>
    /// Hit-sound budget for stacked-drag settle; aliases
    /// <see cref="EffectController.MaxHitSoundsPerFrame"/>.
    /// </summary>
    public const int DragBatchMaxHitSounds = EffectController.MaxHitSoundsPerFrame;

    /// <summary>
    /// Max |ΔeffectiveNoteTime| from the DragCoHit representative (first colliding
    /// non-Miss in list order) for other colliding Drags settled in the same batch.
    /// Keeps same-tick stacks co-clearing without changing which Drag gates Select.
    /// </summary>
    public const float DragStackBatchGapSeconds = 0.015f;

    public Game game;

    public readonly Dictionary<int, FlickNote> FlickingNotes = new Dictionary<int, FlickNote>(); // Finger index to note
    public readonly Dictionary<int, HoldNote> HoldingNotes = new Dictionary<int, HoldNote>(); // Finger index to note
    public readonly List<Note> TouchableDragNotes = new List<Note>(); // Drag head, Drag child, CDrag child
    public readonly List<HoldNote> TouchableHoldNotes = new List<HoldNote>(); // Hold, Long hold (FingerUpdate)
    /// <summary>
    /// Click / CDrag head / Flick / unheld Hold in SpawnedNotes id order.
    /// Rebuilt each frame in <see cref="OnGameUpdate"/> so FingerDown never compares
    /// <c>Model.id</c> on a pooled snapshot entry.
    /// </summary>
    public readonly List<Note> TouchableSelectNotes = new List<Note>();

    private readonly List<Note> hitCandidates = new List<Note>();
    private readonly List<Note> clusterScratch = new List<Note>();
    private readonly List<Note> dragBatchScratch = new List<Note>();

    private void Awake()
    {
        game.onGameUpdate.AddListener(OnGameUpdate);
        game.onGamePaused.AddListener(OnGamePaused);
    }

    public void EnableInput()
    {
        GameTouchInput.FingerDown += OnFingerDown;
        GameTouchInput.FingerUpdate += OnFingerUpdate;
        GameTouchInput.FingerUp += OnFingerUp;
    }

    public void DisableInput()
    {
        GameTouchInput.FingerDown -= OnFingerDown;
        GameTouchInput.FingerUpdate -= OnFingerUpdate;
        GameTouchInput.FingerUp -= OnFingerUp;
    }

    public void OnNoteCollected(Note note)
    {
        if (note.Type == NoteType.Hold || note.Type == NoteType.LongHold)
        {
            // Since you only have 10 fingers, this doesn't need to be optimized
            HoldingNotes.RemoveAll(it => it == note);
        }
        if (note.Type == NoteType.Flick)
        {
            // Since you only have 10 fingers, this doesn't need to be optimized
            FlickingNotes.RemoveAll(it => it == note);
        }
    }

    public void OnGamePaused(Game game)
    {
        HoldingNotes.Values.ForEach(note =>
        {
            note.HoldingFingers.Clear();
        });
        HoldingNotes.Clear();
    }

    public void OnGameUpdate(Game game)
    {
        TouchableDragNotes.Clear();
        TouchableHoldNotes.Clear();
        TouchableSelectNotes.Clear();
        foreach (var id in game.SpawnedNotes.Keys)
        {
            var note = game.SpawnedNotes[id];
            if (!note.HasEmerged || note.IsCleared || note.IsArmed || note.IsCollected) continue;

            if (note.Type == NoteType.DragHead || note.Type == NoteType.DragChild || note.Type == NoteType.CDragChild || note.Type == NoteType.DropDrag)
            {
                TouchableDragNotes.Add(note);
                continue;
            }

            if (note.Type == NoteType.Hold || note.Type == NoteType.LongHold)
            {
                var holdNote = (HoldNote) note;
                if (holdNote.IsHolding) continue;
                TouchableHoldNotes.Add(holdNote);
                TouchableSelectNotes.Add(holdNote);
                continue;
            }

            TouchableSelectNotes.Add(note);
        }
    }

    protected virtual void OnFingerDown(GameFinger finger)
    {
        var pressedPosition = game.camera.orthographic
            ? game.camera.ScreenToWorldPoint(finger.ScreenPosition)
            : game.camera.ScreenToWorldPoint(new Vector3(finger.ScreenPosition.x, finger.ScreenPosition.y, 10));

        // Collect all colliding eligible Drags without settling yet: Immediate vs Armed
        // depends on whether a higher-priority Select candidate claims this fresh Down.
        // Misses still settle immediately and do not arm DragCoHit.
        var acceptedDrag = CollectCollidingDragBatch(pressedPosition);

        // Select is still gated by the accepted Drag before either candidate settles,
        // preserving DragCoHit and cross-page suppression without changing event order.
        var acceptedSelect = FindAcceptedSelect(finger, pressedPosition, acceptedDrag);

        SettleDragBatch(finger.ScreenPosition, deferred: acceptedSelect != null);

        AcceptSelect(acceptedSelect, finger, pressedPosition);
    }

    /// <summary>
    /// Collects a Drag settle batch and returns the DragCoHit representative.
    /// Representative is the first colliding non-Miss in <see cref="TouchableDragNotes"/>
    /// list order — same as legacy <c>FindAcceptedDrag</c>, so Select gating is unchanged.
    /// Additional colliding non-Miss notes within
    /// <see cref="DragStackBatchGapSeconds"/> of that note are added to
    /// <see cref="dragBatchScratch"/> for co-clear. Misses before the representative
    /// still clear immediately; scanning after the representative does not Miss-clear
    /// (legacy returned on first hit).
    /// </summary>
    private Note CollectCollidingDragBatch(Vector2 worldPos)
    {
        dragBatchScratch.Clear();
        Note accepted = null;
        var acceptedTime = 0f;
        foreach (var note in TouchableDragNotes)
        {
            if (!IsTouchableNote(note)) continue;
            if (!note.DoesCollide(worldPos)) continue;
            if (!note.CanHandleTouch()) continue;

            var grade = note.GetTouchGrade();
            if (grade == NoteGrade.None) continue;
            if (grade == NoteGrade.Miss)
            {
                // Legacy FindAcceptedDrag cleared Misses only until the first valid hit.
                if (accepted == null) note.Clear(NoteGrade.Miss);
                continue;
            }

            if (accepted == null)
            {
                accepted = note;
                acceptedTime = EffectiveNoteTime(note);
                dragBatchScratch.Add(note);
                continue;
            }

            if (Math.Abs(EffectiveNoteTime(note) - acceptedTime) > DragStackBatchGapSeconds)
                continue;

            dragBatchScratch.Add(note);
        }

        return accepted;
    }

    /// <summary>
    /// Settles <see cref="dragBatchScratch"/> under the shared per-frame clear-FX /
    /// hit-sound budget (<see cref="EffectController.MaxClearFxPerFrame"/>).
    /// When <paramref name="deferred"/> is false, only the DragCoHit representative
    /// (index 0) uses immediate <see cref="Note.OnTouch"/> -- same as legacy Down.
    /// The rest of the stack uses <see cref="Note.OnTouchDeferred"/>, matching the
    /// legacy FingerUpdate path so early contacts still arm to perfect time.
    /// When <paramref name="deferred"/> is true (Select claimed the Down), every
    /// note in the batch is deferred. Armed notes that later Clear in
    /// <see cref="Note"/> still share the same per-frame FX/SFX budget.
    /// </summary>
    private void SettleDragBatch(Vector2 screenPos, bool deferred)
    {
        if (dragBatchScratch.Count == 0) return;

        var effects = game.effectController;
        effects.BeginClearBatch(DragBatchMaxClearFx, DragBatchMaxHitSounds);
        try
        {
            for (var i = 0; i < dragBatchScratch.Count; i++)
            {
                var note = dragBatchScratch[i];
                if (!IsTouchableNote(note)) continue;
                if (deferred || i > 0) note.OnTouchDeferred(screenPos);
                else note.OnTouch(screenPos);
            }
        }
        finally
        {
            effects.EndClearBatch();
            dragBatchScratch.Clear();
        }
    }

    /// <summary>
    /// Chooses, but does not settle, the Select note that would consume this Down.
    /// This lets Drag decide Immediate versus Armed while keeping Drag settlement first.
    /// </summary>
    private Note FindAcceptedSelect(GameFinger finger, Vector2 pressedPosition, Note acceptedDrag)
    {
        if (!game.IsLoaded || !game.State.IsPlaying) return null;

        // Select in SpawnedNotes id order (TouchableSelectNotes).
        // Flick: list-order bind; flush earlier Click/Hold cluster first (even if Flick is
        // later blocked by DragCoHit). Click / CDrag head / unheld Hold: note-time clusters.
        // All four types pass IsEligibleSelectAfterDrag (DragCoHit + cross-page).
        hitCandidates.Clear();
        foreach (var note in TouchableSelectNotes)
        {
            if (!IsTouchableNote(note)) continue;
            if (!note.DoesCollide(pressedPosition)) continue;

            if (note is FlickNote flickNote)
            {
                var clusterCandidate = FindSelectClickCluster(pressedPosition.x, pressedPosition);
                if (clusterCandidate != null) return clusterCandidate;

                if (FlickingNotes.ContainsKey(finger.Index) || FlickingNotes.ContainsValue(flickNote))
                    continue;
                if (!IsEligibleSelectAfterDrag(flickNote, acceptedDrag)) continue;
                return flickNote;
            }

            if (note is HoldNote holdNote)
            {
                // Lists are per-frame snapshots; same-frame multi-finger rebind uses Update.
                if (holdNote.IsHolding || HoldingNotes.ContainsKey(finger.Index)) continue;
                if (!IsEligibleSelectAfterDrag(holdNote, acceptedDrag)) continue;
                hitCandidates.Add(holdNote);
                continue;
            }

            if (!IsEligibleSelectAfterDrag(note, acceptedDrag)) continue;
            hitCandidates.Add(note);
        }

        return FindSelectClickCluster(pressedPosition.x, pressedPosition);
    }

    /// <summary>
    /// Snapshot entries may outlive the note (Collect clears Model / resets IsCleared).
    /// Gate before touching Model or colliders.
    /// </summary>
    private static bool IsTouchableNote(Note note) =>
        note != null && !note.IsCollected && !note.IsCleared && !note.IsArmed;

    /// <summary>
    /// Same-Down select gate after an optional accepted Drag.
    /// Blocks only when select is more than <see cref="DragCoHitWindowSeconds"/> later
    /// than the Drag (<c>effectiveNoteTime</c>). Also keeps cross-page early suppress.
    /// </summary>
    private bool IsEligibleSelectAfterDrag(Note note, Note acceptedDrag)
    {
        if (acceptedDrag != null)
        {
            var delta = EffectiveNoteTime(note) - EffectiveNoteTime(acceptedDrag);
            if (delta > DragCoHitWindowSeconds) return false;
        }

        if (note.Model.page_index > game.Chart.CurrentPageId &&
            note.Model.start_time - game.Time >
            game.Chart.Model.page_list[game.Chart.CurrentPageId].Duration * 0.5f)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Finds one Click / CDrag-head / Hold from <see cref="hitCandidates"/> using
    /// <see cref="OrderHitCandidatesByNoteTimeClusters"/> without settling it.
    /// </summary>
    private Note FindSelectClickCluster(float touchWorldX, Vector2 pressedPosition)
    {
        if (hitCandidates.Count == 0) return null;
        if (!game.IsLoaded || !game.State.IsPlaying)
        {
            hitCandidates.Clear();
            return null;
        }

        Note accepted = null;
        foreach (var note in OrderHitCandidatesByNoteTimeClusters(touchWorldX))
        {
            if (!IsTouchableNote(note)) continue;

            if (note is HoldNote holdNote)
            {
                if (holdNote.IsHolding) continue;
                accepted = holdNote;
                break;
            }

            if (!note.CanHandleTouch()) continue;
            if (note.GetTouchGrade() == NoteGrade.None) continue;
            accepted = note;
            break;
        }

        hitCandidates.Clear();
        return accepted;
    }

    /// <summary>Settles the previously selected candidate.</summary>
    private bool AcceptSelect(Note note, GameFinger finger, Vector2 pressedPosition)
    {
        if (!IsTouchableNote(note) || !game.IsLoaded || !game.State.IsPlaying) return false;

        if (note is FlickNote flickNote)
        {
            if (FlickingNotes.ContainsKey(finger.Index) || FlickingNotes.ContainsValue(flickNote)) return false;
            FlickingNotes.Add(finger.Index, flickNote);
            flickNote.StartFlicking(pressedPosition);
            return true;
        }

        if (note is HoldNote holdNote)
        {
            if (holdNote.IsHolding || HoldingNotes.ContainsKey(finger.Index)) return false;
            HoldingNotes.Add(finger.Index, holdNote);
            holdNote.UpdateFinger(finger.Index, true);
            return true;
        }

        return note.OnTouch(finger.ScreenPosition);
    }

    protected virtual void OnFingerUpdate(GameFinger finger)
    {
        var pos = game.camera.orthographic
            ? game.camera.ScreenToWorldPoint(finger.ScreenPosition)
            : game.camera.ScreenToWorldPoint(new Vector3(finger.ScreenPosition.x, finger.ScreenPosition.y, 10));

        // Query flick note
        if (FlickingNotes.ContainsKey(finger.Index))
        {
            var flickingNote = FlickingNotes[finger.Index];
            var cleared = flickingNote.UpdateFingerPosition(pos);
            if (cleared) FlickingNotes.Remove(finger.Index);
        }

        // Drag: continuous contact — clear/arm colliding stack batch in one pass
        CollectCollidingDragBatch(pos);
        SettleDragBatch(finger.ScreenPosition, deferred: true);

        // If this is a new finger (Down did not already bind a Hold)
        if (!HoldingNotes.ContainsKey(finger.Index))
        {
            var switchedToNewNote = false; // If the finger holds a new note

            foreach (var note in TouchableHoldNotes)
            {
                if (!IsTouchableNote(note)) continue;
                if (!game.IsLoaded || !game.State.IsPlaying) break;
                if (!note.DoesCollide(pos)) continue;
                HoldingNotes.Add(finger.Index, note);
                note.UpdateFinger(finger.Index, true);
                switchedToNewNote = true;
                break;
            }

            // Query held hold notes (i.e. multiple fingers on the same hold note)
            if (!switchedToNewNote && game.IsLoaded && game.State.IsPlaying)
            {
                foreach (var holdNote in HoldingNotes.Values)
                {
                    if (!IsTouchableNote(holdNote)) continue;
                    if (!holdNote.DoesCollide(pos)) continue;
                    HoldingNotes.Add(finger.Index, holdNote);
                    holdNote.UpdateFinger(finger.Index, true);
                    break;
                }
            }
        }
        else // The finger is already holding a note
        {
            var holdNote = HoldingNotes[finger.Index];

            if (!IsTouchableNote(holdNote))
            {
                HoldingNotes.Remove(finger.Index);
            }
            else if (!holdNote.DoesCollide(pos)) // If holding elsewhere
            {
                holdNote.UpdateFinger(finger.Index, false);
                HoldingNotes.Remove(finger.Index);
            }
        }
    }

    protected virtual void OnFingerUp(GameFinger finger)
    {
        if (HoldingNotes.ContainsKey(finger.Index))
        {
            var holdNote = HoldingNotes[finger.Index];
            holdNote.UpdateFinger(finger.Index, false);
            HoldingNotes.Remove(finger.Index);
        }
        if (FlickingNotes.ContainsKey(finger.Index))
        {
            var pos = game.camera.orthographic
                ? game.camera.ScreenToWorldPoint(finger.ScreenPosition)
                : game.camera.ScreenToWorldPoint(new Vector3(finger.ScreenPosition.x, finger.ScreenPosition.y, 10));

            var flickingNote = FlickingNotes[finger.Index];
            flickingNote.UpdateFingerPosition(pos);
            FlickingNotes.Remove(finger.Index);
        }
    }

    private static float EffectiveNoteTime(Note note) =>
        note.Model.start_time + note.JudgmentOffset;

    private static float RenderedCenterX(Note note)
    {
        var collider = note.Renderer != null ? note.Renderer.GetCollider() : null;
        if (collider != null) return collider.bounds.center.x;
        return note.transform.position.x;
    }

    /// <summary>
    /// Order Click / CDrag-head / Hold candidates for one FingerDown.
    /// Sort by <c>effectiveNoteTime</c>, then split into clusters with span ≤
    /// <see cref="NoteClusterGapSeconds"/> (earlier clusters first; soft fallthrough).
    /// Within a cluster: |Δx| → note time → type (Click/CDrag head before Hold) → id.
    /// Flick is never passed here (list-order bind on the scan path).
    /// Not re-entrant: mutates <see cref="hitCandidates"/> / <c>clusterScratch</c> while yielding.
    /// </summary>
    private IEnumerable<Note> OrderHitCandidatesByNoteTimeClusters(float touchWorldX)
    {
        if (hitCandidates.Count == 0) yield break;

        hitCandidates.Sort((a, b) =>
        {
            var cmp = EffectiveNoteTime(a).CompareTo(EffectiveNoteTime(b));
            if (cmp != 0) return cmp;
            return a.Model.id.CompareTo(b.Model.id);
        });

        var index = 0;
        while (index < hitCandidates.Count)
        {
            var clusterMinTime = EffectiveNoteTime(hitCandidates[index]);
            clusterScratch.Clear();
            clusterScratch.Add(hitCandidates[index]);
            index++;

            while (index < hitCandidates.Count &&
                   EffectiveNoteTime(hitCandidates[index]) - clusterMinTime <= NoteClusterGapSeconds)
            {
                clusterScratch.Add(hitCandidates[index]);
                index++;
            }

            clusterScratch.Sort((a, b) =>
            {
                var dxA = Math.Abs(touchWorldX - RenderedCenterX(a));
                var dxB = Math.Abs(touchWorldX - RenderedCenterX(b));
                var cmp = dxA.CompareTo(dxB);
                if (cmp != 0) return cmp;
                cmp = EffectiveNoteTime(a).CompareTo(EffectiveNoteTime(b));
                if (cmp != 0) return cmp;
                cmp = SelectTypePriority(a).CompareTo(SelectTypePriority(b));
                if (cmp != 0) return cmp;
                return a.Model.id.CompareTo(b.Model.id);
            });

            foreach (var note in clusterScratch)
            {
                yield return note;
            }
        }
    }

    /// <summary>
    /// In-cluster type rank after |Δx| and note time. Lower = preferred.
    /// Click / CDrag head beat Hold on same-time / same-position ties so chart id
    /// alone does not decide who consumes FingerDown.
    /// </summary>
    private static int SelectTypePriority(Note note) =>
        note is HoldNote ? 1 : 0;

}
