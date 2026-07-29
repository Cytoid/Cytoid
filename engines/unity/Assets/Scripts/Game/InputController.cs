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
    /// Flick stays list-order bind; Drag stays list-order scan.
    /// </summary>
    public const float NoteClusterGapSeconds = 0.015f;

    /// <summary>
    /// After a non-Miss Drag clears on the same FingerDown, block Click / CDrag head /
    /// Hold / Flick whose <c>effectiveNoteTime</c> is more than this many seconds later.
    /// Earlier or within-window selects stay eligible.
    /// Wider than <see cref="NoteClusterGapSeconds"/> (30ms vs 15ms) so short Drag+tap
    /// stacks co-clear on one Down without widening note-time clusters.
    /// Replaces legacy collidedDrag + Page.Duration/8.
    /// </summary>
    public const float DragCoHitWindowSeconds = 0.030f;

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
            if (!note.HasEmerged || note.IsCleared || note.IsCollected) continue;

            if (note.Type == NoteType.DragHead || note.Type == NoteType.DragChild || note.Type == NoteType.CDragChild)
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

        // Drag clears first (settlement order) but does not consume the Down for select.
        // Arm DragCoHit only on a real hit grade — TryClear also returns true on Miss.
        Note acceptedDrag = null;
        foreach (var note in TouchableDragNotes)
        {
            if (!IsTouchableNote(note)) continue;
            if (!note.DoesCollide(pressedPosition)) continue;

            // Snapshot before OnTouch: ShouldMiss/CalculateGrade Miss both Clear(Miss) via TryClear.
            var preTouchGrade = note.ShouldMiss() ? NoteGrade.Miss : note.CalculateGrade();
            if (!note.OnTouch(finger.ScreenPosition)) continue;
            if (preTouchGrade == NoteGrade.Miss)
            {
                // Miss on this Down must not arm DragCoHit (would block later select).
                continue;
            }

            acceptedDrag = note;
            break;
        }

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
                if (TryAcceptSelectClickCluster(finger, pressedPosition.x)) return;

                if (FlickingNotes.ContainsKey(finger.Index) || FlickingNotes.ContainsValue(flickNote))
                    continue;
                if (!IsEligibleSelectAfterDrag(flickNote, acceptedDrag)) continue;
                FlickingNotes.Add(finger.Index, flickNote);
                flickNote.StartFlicking(pressedPosition);
                return;
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

        TryAcceptSelectClickCluster(finger, pressedPosition.x);
    }

    /// <summary>
    /// Snapshot entries may outlive the note (Collect clears Model / resets IsCleared).
    /// Gate before touching Model or colliders.
    /// </summary>
    private static bool IsTouchableNote(Note note) =>
        note != null && !note.IsCollected && !note.IsCleared;

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
    /// Accept one Click / CDrag-head / Hold from <see cref="hitCandidates"/> using
    /// <see cref="OrderHitCandidatesByNoteTimeClusters"/>. Hold binds and consumes Down.
    /// </summary>
    private bool TryAcceptSelectClickCluster(GameFinger finger, float touchWorldX)
    {
        if (hitCandidates.Count == 0) return false;
        if (!game.IsLoaded || !game.State.IsPlaying)
        {
            hitCandidates.Clear();
            return false;
        }

        foreach (var note in OrderHitCandidatesByNoteTimeClusters(touchWorldX))
        {
            if (!IsTouchableNote(note)) continue;

            if (note is HoldNote holdNote)
            {
                // Reject holds already bound this frame (stale snapshot).
                if (holdNote.IsHolding || HoldingNotes.ContainsKey(finger.Index)) continue;
                HoldingNotes.Add(finger.Index, holdNote);
                holdNote.UpdateFinger(finger.Index, true);
                hitCandidates.Clear();
                return true;
            }

            if (!note.OnTouch(finger.ScreenPosition)) continue;
            hitCandidates.Clear();
            return true;
        }

        hitCandidates.Clear();
        return false;
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

        // Drag: continuous contact — list-order scan
        foreach (var note in TouchableDragNotes)
        {
            if (!IsTouchableNote(note)) continue;
            if (!note.DoesCollide(pos)) continue;
            if (!note.OnTouch(finger.ScreenPosition)) continue;
            break;
        }

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
