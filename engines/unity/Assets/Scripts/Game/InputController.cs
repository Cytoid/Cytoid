using System;
using System.Collections.Generic;
using UnityEngine;

public class InputController : MonoBehaviour
{
    /// <summary>
    /// Max note-to-note span (seconds) for one FingerDown hit cluster
    /// (true or pseudo simultaneous press). Applies to Click / CDrag head / unheld Hold.
    /// Candidates are re-clustered each Down by <c>effectiveNoteTime</c> span <= this gap
    /// (no adjacent-gap chain expansion). Soft fallthrough across clusters remains.
    /// In-cluster rank: see <see cref="OrderHitCandidatesByNoteTimeClusters"/>.
    /// Flick joins Click/Hold note-time clusters; Drag settles all colliding eligible notes per input.
    /// </summary>
    public const float NoteClusterGapSeconds = 0.015f;

    /// <summary>
    /// After an ordinary non-Miss Drag is accepted on the same FingerDown, block Click / CDrag head /
    /// Hold / Flick whose <c>effectiveNoteTime</c> is more than this many seconds later.
    /// Earlier or within-window selects stay eligible.
    /// Wider than <see cref="NoteClusterGapSeconds"/> (30ms vs 15ms) so short Drag+tap
    /// stacks co-clear on one Down without widening note-time clusters.
    /// DropDrag shares the co-hit exemption but uses a landing/time guard for later Selects.
    /// Replaces legacy collidedDrag + Page.Duration/8 for ordinary Drags.
    /// </summary>
    public const float DragCoHitWindowSeconds = 0.030f;

    /// <summary>
    /// Clear FX budget for one co-located drag-stack settle; aliases
    /// <see cref="EffectController.MaxClearFxPerDragBatch"/>.
    /// </summary>
    public const int DragBatchMaxClearFx = EffectController.MaxClearFxPerDragBatch;

    /// <summary>
    /// Hit-sound budget for one co-located drag-stack settle; aliases
    /// <see cref="EffectController.MaxHitSoundsPerDragBatch"/>.
    /// </summary>
    public const int DragBatchMaxHitSounds = EffectController.MaxHitSoundsPerDragBatch;

    /// <summary>
    /// Max |delta effectiveNoteTime| from the DragCoHit representative (first colliding
    /// non-Miss in list order) for other colliding Drags settled in the same batch.
    /// Keeps same-tick stacks co-clearing without changing which Drag gates Select.
    /// </summary>
    public const float DragStackBatchGapSeconds = 0.015f;

    /// <summary>
    /// A recording only binds for release settlement after its finger has moved this far
    /// (judgement-world units) from its press point: "swiped past". A stationary tap that
    /// merely overlaps a neighbouring drag's hitbox still records it, but that recording
    /// resolves at the note's own trigger point instead of popping at the tap's lift
    /// (cyanine record 21348400: taps on 28/46 also popped 30/47, 330ms early).
    /// </summary>
    public const float SwipeBindMoveThreshold = 0.05f;

    /// <summary>
    /// A DropDrag press settles its representative immediately only while within this
    /// window ahead of the trigger point (on-time feel). Earlier DropDrag presses merely
    /// record the note (arm) and it resolves at its own trigger point -- a press aimed
    /// at a just-consumed neighbour whose hitbox overlaps an earlier DropDrag must not
    /// pop that note far above the line (cyanine record 21349071: 17 settled 116ms
    /// early because 15 had been recorded by an overlapping stationary hold and already
    /// tick-resolved when the player's own press for 15 landed). Ordinary travelling
    /// drags are exempt: they are chased with the scan line and keep the instant settle.
    /// </summary>
    public const float PressSettleEarlyWindowSeconds = 0.070f;

    public Game game;

    public readonly Dictionary<int, FlickNote> FlickingNotes = new Dictionary<int, FlickNote>(); // Finger index to note
    public readonly Dictionary<int, HoldNote> HoldingNotes = new Dictionary<int, HoldNote>(); // Finger index to note
    public readonly List<Note> TouchableDragNotes = new List<Note>(); // Drag head, Drag child, CDrag child, DropDrag
    /// <summary>
    /// Drag notes currently recorded (armed) and not yet settled -- a per-frame
    /// snapshot like <see cref="TouchableDragNotes"/>. Armed drags are not settle
    /// candidates (they stay absent from <see cref="TouchableDragNotes"/>) but a
    /// Down touching an earlier one continues that in-flight chain instead of
    /// settling the next note at tap time (see <see cref="PressedOnInFlightDragChain"/>).
    /// Stack followers are absent; their primaries carry the shared position.
    /// </summary>
    public readonly List<Note> ArmedDragNotes = new List<Note>(); // Recorded (armed) drags
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
    /// <summary>
    /// Drag notes recorded (armed) by each finger's swipe contact -- recordings made while
    /// the finger was still at its press point never bind (see
    /// <see cref="SwipeBindMoveThreshold"/>). Bound notes settle when that finger lifts
    /// (<see cref="SettleArmedDrags"/>) or at their own trigger points, whichever comes
    /// first. DropDrag notes are never bound here: they settle only at their own trigger
    /// points. Entries for notes that already settled are pruned lazily.
    /// </summary>
    private readonly Dictionary<int, List<Note>> armedDragsByFinger = new Dictionary<int, List<Note>>();
    /// <summary>Press-point (judgement-world) per finger; drives swipe detection for release binding.</summary>
    private readonly Dictionary<int, Vector3> fingerDownPositions = new Dictionary<int, Vector3>();
    private readonly Dictionary<int, DropDragGuard> clearedDropGuards = new Dictionary<int, DropDragGuard>();
    private readonly List<DropDragGuard> dropGuardsForDown = new List<DropDragGuard>();
    private readonly List<int> expiredDropGuardIds = new List<int>();
    private bool captureDropGuards;

    private void Awake()
    {
        game.onGameUpdate.AddListener(OnGameUpdate);
        game.onGamePaused.AddListener(OnGamePaused);
        game.onGameStarted.AddListener(OnGameStarted);
        game.onNoteJudged.AddListener(CaptureClearedDropDrag);
    }

    public void EnableInput()
    {
        ResetDropGuards();
        captureDropGuards = true;
        GameTouchInput.FingerDown += OnFingerDown;
        GameTouchInput.FingerUpdate += OnFingerUpdate;
        GameTouchInput.FingerUp += OnFingerUp;
    }

    public void DisableInput()
    {
        captureDropGuards = false;
        ResetDropGuards();
        GameTouchInput.FingerDown -= OnFingerDown;
        GameTouchInput.FingerUpdate -= OnFingerUpdate;
        GameTouchInput.FingerUp -= OnFingerUp;
        ResetBindingsWithoutJudgment();
    }

    private void OnDestroy()
    {
        GameTouchInput.FingerDown -= OnFingerDown;
        GameTouchInput.FingerUpdate -= OnFingerUpdate;
        GameTouchInput.FingerUp -= OnFingerUp;
        if (game == null) return;
        game.onGameUpdate.RemoveListener(OnGameUpdate);
        game.onGamePaused.RemoveListener(OnGamePaused);
        game.onGameStarted.RemoveListener(OnGameStarted);
        game.onNoteJudged.RemoveListener(CaptureClearedDropDrag);
    }

    private void OnGameStarted(Game _) => ResetDropGuards();

    /// <summary>
    /// Clears snapshots for an explicit new play or chart seek. Small DSP clock
    /// corrections during the same play must not discard unexpired guards.
    /// </summary>
    public void ResetDropGuards()
    {
        clearedDropGuards.Clear();
        dropGuardsForDown.Clear();
        expiredDropGuardIds.Clear();
    }

    private void CaptureClearedDropDrag(Game g, Note note, JudgeData data)
    {
        if (!captureDropGuards || note == null || note.Model == null ||
            note.Type != NoteType.DropDrag || data == null ||
            data.Grade == NoteGrade.None || data.Grade == NoteGrade.Miss) return;
        var guard = DropNoteIsolation.Snapshot(note);
        if (g.Time < guard.EffectiveTime + DropNoteIsolation.LateWindowSeconds)
            clearedDropGuards[guard.NoteId] = guard;
    }

    private void PruneDropGuards(float now)
    {
        // Chart time can move backward during DSP synchronization. Only lifecycle
        // resets discard all guards; normal updates remove expired entries only.
        expiredDropGuardIds.Clear();
        foreach (var pair in clearedDropGuards)
            if (now >= pair.Value.EffectiveTime + DropNoteIsolation.LateWindowSeconds)
                expiredDropGuardIds.Add(pair.Key);
        foreach (var id in expiredDropGuardIds) clearedDropGuards.Remove(id);
    }

    private void BuildDropGuardsForDown()
    {
        PruneDropGuards(game.Time);
        dropGuardsForDown.Clear();
        // Armed notes are deliberately absent from TouchableDragNotes. Read the
        // live pool, then append immutable cleared snapshots (including same-frame clears).
        foreach (var note in game.SpawnedNotes.Values)
        {
            if (note == null || note.Model == null || note.IsCollected || note.IsCleared ||
                note.Type != NoteType.DropDrag || !note.HasEmerged) continue;
            if (!note.IsArmed)
            {
                if (!note.CanHandleTouch()) continue;
                var grade = note.GetTouchGrade();
                if (grade == NoteGrade.None || grade == NoteGrade.Miss) continue;
            }
            dropGuardsForDown.Add(DropNoteIsolation.Snapshot(note));
        }
        foreach (var guard in clearedDropGuards.Values) dropGuardsForDown.Add(guard);
    }

    /// <summary>
    /// Release Hold/Flick finger ownership without producing new judgments. Armed drag
    /// bindings drop too: those notes stay armed and settle at their own trigger points
    /// via the judgment tick. Used by pause, Fail/Complete/Dispose via
    /// <see cref="DisableInput"/>.
    /// </summary>
    public void ResetBindingsWithoutJudgment()
    {
        HoldingNotes.Values.ForEach(note =>
        {
            note.HoldingFingers.Clear();
        });
        HoldingNotes.Clear();

        FlickingNotes.Values.ForEach(note =>
        {
            if (!note.IsCleared) note.StopFlicking();
        });
        FlickingNotes.Clear();

        armedDragsByFinger.Clear();
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
        ResetBindingsWithoutJudgment();
    }

    public void OnGameUpdate(Game game)
    {
        PruneDropGuards(game.Time);
        TouchableDragNotes.Clear();
        ArmedDragNotes.Clear();
        TouchableHoldNotes.Clear();
        TouchableSelectNotes.Clear();
        foreach (var id in game.SpawnedNotes.Keys)
        {
            var note = game.SpawnedNotes[id];
            if (note.IsCleared || note.IsCollected) continue;
            if (note.IsArmed)
            {
                // Armed drags are in-flight recordings, not settle candidates. Keeping
                // them in a dedicated list lets a later Down detect chain continuation
                // without rescanning the pool; they stay absent from TouchableDragNotes.
                if (note.Type == NoteType.DragHead || note.Type == NoteType.DragChild ||
                    note.Type == NoteType.CDragChild || note.Type == NoteType.DropDrag)
                {
                    if (!note.IsDragStackFollower) ArmedDragNotes.Add(note);
                }
                continue;
            }
            if (!note.HasEmerged) continue;

            if (note.Type == NoteType.DragHead || note.Type == NoteType.DragChild || note.Type == NoteType.CDragChild || note.Type == NoteType.DropDrag)
            {
                // Stack followers share the primary collider — only scan primaries here.
                if (note.IsDragStackFollower) continue;
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

            // CDragHead stays on the normal Select path (not stack-collapsed).
            TouchableSelectNotes.Add(note);
        }
    }

    protected virtual void OnFingerDown(GameFinger finger)
    {
        var pressedPosition = game.camera.orthographic
            ? game.camera.ScreenToWorldPoint(finger.ScreenPosition)
            : game.camera.ScreenToWorldPoint(new Vector3(finger.ScreenPosition.x, finger.ScreenPosition.y, 10));
        var downSequence = game.PlayRecordRecorder?.Record(finger.Index, "down", pressedPosition) ?? -1;
        fingerDownPositions[finger.Index] = pressedPosition;

        BuildDropGuardsForDown();

        // Collect + settle under one drag-stack FX/SFX budget so Miss clears during
        // collection share the same caps as the co-located non-Miss batch.
        var effects = game.effectController;
        effects.BeginClearBatch(DragBatchMaxClearFx, DragBatchMaxHitSounds);
        Note acceptedSelect;
        // Every settlement this press produces (drag representative, batch Misses,
        // the accepted Select) shares the press attribution; a null recorder scope is a no-op.
        using (game.PlayRecordRecorder?.PushJudgmentContext(
            GamePlayRecordWire.CausePress, finger.Index,
            downSequence >= 0 ? downSequence : (long?) null))
        {
            try
            {
                // Collect all colliding eligible Drags without settling yet: Immediate vs Armed
                // depends on whether a higher-priority Select candidate claims this fresh Down.
                // Misses still settle immediately and do not arm DragCoHit.
                var acceptedDrag = CollectCollidingDragBatch(pressedPosition);
                // Collider miss: collect occluding DropDrags first so every Select on this
                // Down shares one DragCoHit representative (and one batch window).
                if (acceptedDrag == null)
                    acceptedDrag = CollectOccludingDropDragBatch(finger, pressedPosition);

                // Filter Selects before either candidate settles. DropDrag uses the same
                // spatial/time guard in every state; ordinary Drags retain DragCoHit.
                acceptedSelect = FindAcceptedSelect(finger, pressedPosition, acceptedDrag);

                // A press onto an in-flight (already recorded) chain continues the recording:
                // the series' first note is armed, so this Down must not settle the next one early.
                var continuesInFlightChain = PressedOnInFlightDragChain(pressedPosition, acceptedDrag);
                SettleDragBatch(finger.ScreenPosition,
                    deferred: acceptedSelect != null || continuesInFlightChain,
                    fingerIndex: finger.Index, bindRecordings: false,
                    inputSequence: downSequence >= 0 ? downSequence : (long?) null);
            }
            finally
            {
                effects.EndClearBatch();
            }

            AcceptSelect(acceptedSelect, finger, pressedPosition);
        }
    }

    /// <summary>
    /// Collects a Drag settle batch and returns the DragCoHit representative.
    /// Representative is the first colliding non-Miss in <see cref="TouchableDragNotes"/>
    /// list order -- same as legacy <c>FindAcceptedDrag</c>. Ordinary Drag gating is unchanged.
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

            // Expand co-located stack members that share this host's collider.
            if (note.DragStack != null && note.DragStack.IsPrimary(note))
            {
                ExpandDragStackIntoBatch(note.DragStack, ref accepted, ref acceptedTime);
                continue;
            }

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

    private void ExpandDragStackIntoBatch(DragStackHost host, ref Note accepted, ref float acceptedTime)
    {
        for (var i = 0; i < host.Members.Count; i++)
        {
            var note = host.Members[i];
            if (!IsTouchableNote(note)) continue;
            if (!note.CanHandleTouch()) continue;

            var grade = note.GetTouchGrade();
            if (grade == NoteGrade.None) continue;
            if (grade == NoteGrade.Miss)
            {
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

            if (!dragBatchScratch.Contains(note))
                dragBatchScratch.Add(note);
        }
    }

    /// <summary>
    /// Settles <see cref="dragBatchScratch"/>. Caller must hold
    /// <see cref="EffectController.BeginClearBatch"/> so immediate Clears (including
    /// Misses collected earlier) share one co-located drag-stack FX/SFX budget.
    /// Press semantics: only the representative (slot 0) settles immediately, and only
    /// when no Select claimed this Down (<paramref name="deferred"/> false), no earlier
    /// colliding drag is already armed (an in-flight chain), and -- for DropDrag only --
    /// the press is within <see cref="PressSettleEarlyWindowSeconds"/> of the trigger
    /// point (or past it) -- with its contact grade. Ordinary travelling drags always
    /// settle immediately (instant feel). Every other contact (far-early DropDrag
    /// representatives, stack members, Select-deferred or in-flight-continuation
    /// batches, swipe/hold updates) is recorded via <see cref="Note.OnTouchDeferred"/>
    /// and settles when the contacting finger lifts or at the note's own trigger point,
    /// whichever comes first -- but only recordings made after the finger moved from its
    /// press point (<paramref name="bindRecordings"/>; Down-path recordings never bind)
    /// bind for release settlement. Stationary-contact recordings and DropDrag (always)
    /// resolve exclusively at their own trigger points.
    /// Multi-note batches that arm mark <see cref="Note.MarkDragStackBudget"/> with one
    /// shared stack id so later co-clears share caps via
    /// <see cref="EffectController.EnterDragStackClear"/> without affecting
    /// Click/Flick/Hold/Auto feedback.
    /// </summary>
    private void SettleDragBatch(Vector2 screenPos, bool deferred, int fingerIndex, bool bindRecordings,
        long? inputSequence)
    {
        if (dragBatchScratch.Count == 0) return;

        var budgetStack = dragBatchScratch.Count > 1;
        var stackBudgetId = budgetStack
            ? game.effectController.AllocateDragStackBudgetId()
            : 0;
        for (var i = 0; i < dragBatchScratch.Count; i++)
        {
            var note = dragBatchScratch[i];
            if (!IsTouchableNote(note)) continue;
            var dropDragEarlyPress = note.Type == NoteType.DropDrag &&
                                     game.Time < EffectiveNoteTime(note) - PressSettleEarlyWindowSeconds;
            if (deferred || i > 0 || dropDragEarlyPress)
            {
                note.OnTouchDeferred(screenPos);
                if (note.IsArmed)
                {
                    // Drop drags never release-settle; and only swipe recordings bind --
                    // a stationary tap overlapping a neighbour's hitbox records it for its
                    // own trigger point instead of popping it at the tap's lift.
                    if (bindRecordings && note.Type != NoteType.DropDrag) BindArmedDrag(fingerIndex, note);
                    if (budgetStack) note.MarkDragStackBudget(stackBudgetId);
                    game.PlayRecordRecorder?.RecordArm(fingerIndex, note, inputSequence);
                }
            }
            else note.OnTouch(screenPos);
        }

        dragBatchScratch.Clear();
    }

    /// <summary>
    /// Records a freshly armed drag under the finger whose contact armed it, so that
    /// finger's release settles it with its recorded grade. A note can only be armed
    /// once; removing it from every other finger's list first also drops stale entries
    /// left by notes that already settled and were pool-reused.
    /// </summary>
    private void BindArmedDrag(int fingerIndex, Note note)
    {
        foreach (var entry in armedDragsByFinger)
        {
            if (entry.Key != fingerIndex) entry.Value.Remove(note);
        }
        if (!armedDragsByFinger.TryGetValue(fingerIndex, out var list))
        {
            list = new List<Note>();
            armedDragsByFinger[fingerIndex] = list;
        }
        if (!list.Contains(note)) list.Add(note);
    }

    /// <summary>
    /// Settles drag notes this finger recorded but whose trigger points have not yet
    /// arrived: releasing the finger judges them immediately with their contact grades.
    /// Notes already settled at their trigger points (or pool-reused) no-op in
    /// <see cref="Note.SettleArmed"/>.
    /// </summary>
    private void SettleArmedDrags(int fingerIndex)
    {
        if (!armedDragsByFinger.TryGetValue(fingerIndex, out var list)) return;
        armedDragsByFinger.Remove(fingerIndex);
        if (list.Count == 0 || !game.IsLoaded || !game.State.IsPlaying) return;

        var effects = game.effectController;
        effects.BeginClearBatch(DragBatchMaxClearFx, DragBatchMaxHitSounds);
        try
        {
            foreach (var note in list) note.SettleArmed();
        }
        finally
        {
            effects.EndClearBatch();
            list.Clear();
        }
    }

    /// <summary>
    /// True when an already-armed drag collides with this press ahead of the batch
    /// representative: the press lands on a chain that is already being recorded, so
    /// it continues the recording instead of settling the next note at tap time.
    /// Iterates the per-frame <see cref="ArmedDragNotes"/> snapshot; entries are
    /// re-checked live because a snapshot note may settle in the same frame's tick
    /// after the rebuild.
    /// </summary>
    private bool PressedOnInFlightDragChain(Vector2 worldPos, Note representative)
    {
        if (representative == null) return false;
        for (var i = 0; i < ArmedDragNotes.Count; i++)
        {
            var note = ArmedDragNotes[i];
            if (note == null || note.Model == null || note.IsCollected || note.IsCleared || !note.IsArmed) continue;
            if (!note.DoesCollide(worldPos)) continue;
            if (EffectiveNoteTime(note) < EffectiveNoteTime(representative)) return true;
        }
        return false;
    }

    /// <summary>
    /// Chooses, but does not settle, the Select note that would consume this Down.
    /// Click / Flick / Hold share note-time clusters and rendered-X order.
    /// This lets Drag decide Immediate versus Armed while keeping Drag settlement first.
    /// </summary>
    private Note FindAcceptedSelect(GameFinger finger, Vector2 pressedPosition, Note acceptedDrag)
    {
        if (!game.IsLoaded || !game.State.IsPlaying) return null;

        hitCandidates.Clear();
        foreach (var note in TouchableSelectNotes)
        {
            if (!IsTouchableNote(note)) continue;
            if (!note.DoesCollide(pressedPosition)) continue;

            if (note is FlickNote flickNote)
            {
                if (FlickingNotes.ContainsKey(finger.Index) || FlickingNotes.ContainsValue(flickNote))
                    continue;
                if (flickNote.IsFlicking) continue;
                if (!IsEligibleSelectAfterDrag(flickNote, acceptedDrag, pressedPosition)) continue;
                hitCandidates.Add(flickNote);
                continue;
            }

            if (note is HoldNote holdNote)
            {
                // Lists are per-frame snapshots; same-frame multi-finger rebind uses Update.
                if (holdNote.IsHolding || HoldingNotes.ContainsKey(finger.Index)) continue;
                if (!IsEligibleSelectAfterDrag(holdNote, acceptedDrag, pressedPosition)) continue;
                hitCandidates.Add(holdNote);
                continue;
            }

            if (!IsEligibleSelectAfterDrag(note, acceptedDrag, pressedPosition)) continue;
            hitCandidates.Add(note);
        }

        return FindSelectCluster(finger, pressedPosition.x);
    }

    /// <summary>
    /// Snapshot entries may outlive the note (Collect clears Model / resets IsCleared).
    /// Gate before touching Model or colliders.
    /// </summary>
    private static bool IsTouchableNote(Note note) =>
        note != null && note.Model != null && !note.IsCollected && !note.IsCleared && !note.IsArmed;

    /// <summary>
    /// Same-Down select gate after an optional accepted Drag.
    /// Ordinary Drags keep the 30ms gate. DropDrags use their uniform landing/time
    /// guard so a fresh late Drag cannot block an on-time Click. Keeps cross-page suppression.
    /// </summary>
    private bool IsEligibleSelectAfterDrag(Note note, Note acceptedDrag, Vector2 tap)
    {
        if (acceptedDrag != null && acceptedDrag.Type != NoteType.DropDrag)
        {
            var delta = EffectiveNoteTime(note) - EffectiveNoteTime(acceptedDrag);
            if (delta > DragCoHitWindowSeconds) return false;
        }

        var selectTime = EffectiveNoteTime(note);
        var radius = DropNoteIsolation.WorldHitboxRadius(note);
        foreach (var guard in dropGuardsForDown)
        {
            if (DropNoteIsolation.BlocksSelect(guard, selectTime, radius, tap,
                    game.Time, game.Chart.CurrentPageId)) return false;
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
    /// DropDrag stand-in when the collider missed. Queues every occluding DropDrag
    /// within <see cref="DragStackBatchGapSeconds"/> of the first match (same window
    /// as <see cref="CollectCollidingDragBatch"/>) and returns that representative
    /// for settlement. Select eligibility uses the uniform DropDrag guard instead
    /// of an unconditional representative-to-Select time gap.
    /// </summary>
    private Note CollectOccludingDropDragBatch(GameFinger finger, Vector2 tap)
    {
        Note accepted = null;
        var acceptedTime = 0f;
        foreach (var drag in TouchableDragNotes)
        {
            if (drag.Type != NoteType.DropDrag) continue;
            if (!IsTouchableNote(drag) || !drag.CanHandleTouch()) continue;
            var grade = drag.GetTouchGrade();
            if (grade == NoteGrade.None || grade == NoteGrade.Miss) continue;
            if (!OccludesAnyCollidingSelect(drag, finger, tap)) continue;

            if (accepted == null)
            {
                accepted = drag;
                acceptedTime = EffectiveNoteTime(drag);
                dragBatchScratch.Add(drag);
                continue;
            }

            if (Math.Abs(EffectiveNoteTime(drag) - acceptedTime) > DragStackBatchGapSeconds)
                continue;

            dragBatchScratch.Add(drag);
        }

        return accepted;
    }

    private bool OccludesAnyCollidingSelect(Note drag, GameFinger finger, Vector2 tap)
    {
        foreach (var select in TouchableSelectNotes)
        {
            if (!IsCollidingSelectCandidate(select, finger, tap)) continue;
            if (DropNoteIsolation.BlocksSelect(DropNoteIsolation.Snapshot(drag),
                    EffectiveNoteTime(select), DropNoteIsolation.WorldHitboxRadius(select),
                    tap, game.Time, game.Chart.CurrentPageId)) return true;
        }

        return false;
    }

    private bool IsCollidingSelectCandidate(Note note, GameFinger finger, Vector2 tap)
    {
        if (!IsTouchableNote(note)) return false;
        if (!note.DoesCollide(tap)) return false;

        if (note is FlickNote flickNote)
        {
            if (FlickingNotes.ContainsKey(finger.Index) || FlickingNotes.ContainsValue(flickNote))
                return false;
            if (flickNote.IsFlicking) return false;
        }

        if (note is HoldNote holdNote)
        {
            if (holdNote.IsHolding || HoldingNotes.ContainsKey(finger.Index)) return false;
        }

        return true;
    }

    /// <summary>
    /// Finds one Click / CDrag-head / Flick / Hold from <see cref="hitCandidates"/> using
    /// <see cref="OrderHitCandidatesByNoteTimeClusters"/> without settling it.
    /// Revalidates cleared / reserved / holding state for same-frame multi-finger Downs.
    /// </summary>
    private Note FindSelectCluster(GameFinger finger, float touchWorldX)
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

            if (note is FlickNote flickNote)
            {
                if (FlickingNotes.ContainsKey(finger.Index) || FlickingNotes.ContainsValue(flickNote))
                    continue;
                if (flickNote.IsFlicking) continue;
                accepted = flickNote;
                break;
            }

            if (note is HoldNote holdNote)
            {
                if (holdNote.IsHolding || HoldingNotes.ContainsKey(finger.Index)) continue;
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
            if (flickNote.IsFlicking) return false;
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
        var updateSequence = game.PlayRecordRecorder?.Record(finger.Index, "update", pos) ?? -1;

        using (game.PlayRecordRecorder?.PushJudgmentContext(
            GamePlayRecordWire.CauseContact, finger.Index,
            updateSequence >= 0 ? updateSequence : (long?) null))
        {
            if (FlickingNotes.ContainsKey(finger.Index))
            {
                var flickingNote = FlickingNotes[finger.Index];
                if (!IsTouchableNote(flickingNote))
                {
                    flickingNote.StopFlicking();
                    FlickingNotes.Remove(finger.Index);
                }
                else
                {
                    var result = flickingNote.UpdateFingerPosition(pos);
                    if (result == FlickFingerResult.Cleared)
                        FlickingNotes.Remove(finger.Index);
                }
            }

            // Drag: continuous contact -- record (arm) colliding stack batch in one pass.
            // Only recordings made while the finger has moved away from its press point bind
            // for release settlement; stationary-contact recordings wait for their triggers.
            var swipeBind = fingerDownPositions.TryGetValue(finger.Index, out var downPos) &&
                            (pos - downPos).sqrMagnitude > SwipeBindMoveThreshold * SwipeBindMoveThreshold;
            var effects = game.effectController;
            effects.BeginClearBatch(DragBatchMaxClearFx, DragBatchMaxHitSounds);
            try
            {
                CollectCollidingDragBatch(pos);
                SettleDragBatch(finger.ScreenPosition, deferred: true,
                    fingerIndex: finger.Index, bindRecordings: swipeBind,
                    inputSequence: updateSequence >= 0 ? updateSequence : (long?) null);
            }
            finally
            {
                effects.EndClearBatch();
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
    }

    protected virtual void OnFingerUp(GameFinger finger)
    {
        var pos = game.camera.orthographic
            ? game.camera.ScreenToWorldPoint(finger.ScreenPosition)
            : game.camera.ScreenToWorldPoint(new Vector3(finger.ScreenPosition.x, finger.ScreenPosition.y, 10));
        var upSequence = game.PlayRecordRecorder?.Record(finger.Index, "up", pos) ?? -1;

        using (game.PlayRecordRecorder?.PushJudgmentContext(
            GamePlayRecordWire.CauseRelease, finger.Index,
            upSequence >= 0 ? upSequence : (long?) null))
        {
            if (HoldingNotes.ContainsKey(finger.Index))
            {
                var holdNote = HoldingNotes[finger.Index];
                holdNote.UpdateFinger(finger.Index, false);
                HoldingNotes.Remove(finger.Index);
            }
            if (FlickingNotes.ContainsKey(finger.Index))
            {
                var flickingNote = FlickingNotes[finger.Index];
                flickingNote.ReleaseFinger(pos);
                FlickingNotes.Remove(finger.Index);
            }

            // Drags this finger recorded settle now unless their trigger points already did.
            SettleArmedDrags(finger.Index);
            fingerDownPositions.Remove(finger.Index);
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
    /// Order Click / CDrag-head / Flick / Hold candidates for one FingerDown.
    /// Sort by <c>effectiveNoteTime</c>, then split into clusters with span &lt;=
    /// <see cref="NoteClusterGapSeconds"/> (earlier clusters first; soft fallthrough).
    /// Within a cluster: |dx| -&gt; note time -&gt; kind (Click, Flick, Hold) -&gt; id.
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
    /// In-cluster type rank after |dx| and note time. Lower = preferred.
    /// Click-like = 0, Flick = 1, Hold = 2.
    /// </summary>
    private static int SelectTypePriority(Note note)
    {
        if (note is HoldNote) return 2;
        if (note is FlickNote) return 1;
        return 0;
    }

}
