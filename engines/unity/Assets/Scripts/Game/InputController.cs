using System;
using System.Collections.Generic;
using UnityEngine;

public class InputController : MonoBehaviour
{
    /// <summary>
    /// Max note-to-note span (seconds) for a same-beat hit cluster (真双押 / 伪双).
    /// Used for Click / CDrag head / unheld Hold on FingerDown (discrete click consume).
    /// Within a cluster Click/CDrag head/Hold share |Δx| priority; across clusters note-time order wins.
    /// Flick keeps original list-order bind; Drag stays on list-order scan.
    /// Each FingerDown re-clusters the remaining collided candidates by effectiveNoteTime
    /// span ≤ this gap (no adjacent-gap chain expansion within that candidate set).
    /// Soft fallthrough to later clusters remains.
    /// </summary>
    public const float NoteClusterGapSeconds = 0.015f;

    /// <summary>
    /// After a non-Miss Drag clears on FingerDown, select notes (Click / CDrag head / Hold / Flick)
    /// whose effectiveNoteTime is more than this many seconds later than the accepted Drag
    /// are blocked. Selects within the window (or earlier than the Drag) still proceed.
    /// Independent of <see cref="NoteClusterGapSeconds"/> but same 15ms default.
    /// Replaces the legacy collidedDrag + Page.Duration/8 far-note heuristic.
    /// </summary>
    public const float DragCoHitWindowSeconds = 0.015f;

    public Game game;

    public readonly Dictionary<int, FlickNote> FlickingNotes = new Dictionary<int, FlickNote>(); // Finger index to note
    public readonly Dictionary<int, HoldNote> HoldingNotes = new Dictionary<int, HoldNote>(); // Finger index to note
    public readonly List<Note> TouchableDragNotes = new List<Note>(); // Drag head, Drag child, CDrag child
    public readonly List<HoldNote> TouchableHoldNotes = new List<HoldNote>(); // Hold, Long hold (FingerUpdate)
    /// <summary>
    /// Click / CDrag head / Flick / unheld Hold in SpawnedNotes id order.
    /// Built in <see cref="OnGameUpdate"/> so FingerDown never merge-compares Model.id
    /// on a possibly pooled snapshot entry.
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
        // Record acceptedDrag only for a real hit grade — TryClear also returns true on Miss.
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
                // Miss-settled on this Down must not arm DragCoHit (would block later select).
                continue;
            }

            acceptedDrag = note;
            break;
        }

        // Select notes: pre-merged SpawnedNotes id order (TouchableSelectNotes).
        // Flick keeps list-order bind (#187): only earlier candidates are flushed before
        // a Flick bind. Click/CDrag head + unheld Hold share note-time clusters.
        // Click / CDrag head / Hold / Flick all pass IsEligibleSelectAfterDrag
        // (DragCoHit window + cross-page). Blocked Flick still flushes earlier candidates.
        hitCandidates.Clear();
        foreach (var note in TouchableSelectNotes)
        {
            if (!IsTouchableNote(note)) continue;
            if (!note.DoesCollide(pressedPosition)) continue;

            if (note is FlickNote flickNote)
            {
                // Flush earlier Click/Hold even when this Flick is later blocked by DragCoHit.
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
                // Live check: lists are per-frame snapshots; same-frame multi-finger
                // rebind stays on FingerUpdate.
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
    /// Snapshot entries may outlive the note: Collect clears Model and resets IsCleared.
    /// Always gate on IsCollected before touching Model / colliders.
    /// </summary>
    private static bool IsTouchableNote(Note note) =>
        note != null && !note.IsCollected && !note.IsCleared;

    /// <summary>
    /// Select gate after an optional accepted Drag on the same FingerDown.
    /// Blocks only when select is more than <see cref="DragCoHitWindowSeconds"/> later
    /// than the Drag (effectiveNoteTime). Earlier / within-window selects remain eligible.
    /// Also keeps the historical cross-page early suppress.
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
    /// Accept a pending Click/CDrag-head/Hold cluster (note-time + |Δx|).
    /// Clears <see cref="hitCandidates"/>. Hold binds and consumes the Down event.
    /// </summary>
    private bool TryAcceptSelectClickCluster(GameFinger finger, float touchWorldX)
    {
        if (hitCandidates.Count == 0) return false;

        foreach (var note in OrderHitCandidatesByNoteTimeClusters(touchWorldX))
        {
            if (!IsTouchableNote(note)) continue;

            if (note is HoldNote holdNote)
            {
                // Reject holds already bound this frame (stale snapshot).
                if (holdNote.IsHolding || HoldingNotes.ContainsKey(finger.Index)) continue;
                if (!game.IsLoaded || !game.State.IsPlaying) continue;
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
    /// Yield Click/CDrag-head/Hold candidates in beat order: each FingerDown re-clusters
    /// the current remaining candidates by effectiveNoteTime span ≤
    /// <see cref="NoteClusterGapSeconds"/> (no chain expansion within that set), then
    /// processes earlier clusters first; within a cluster prefer closer rendered center X,
    /// then time, then id (Hold competes with Click/CDrag head on |Δx|). Soft fallthrough:
    /// caller continues when Accept fails. Flick is never passed here — list-order bind.
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
                return a.Model.id.CompareTo(b.Model.id);
            });

            foreach (var note in clusterScratch)
            {
                yield return note;
            }
        }
    }

}
