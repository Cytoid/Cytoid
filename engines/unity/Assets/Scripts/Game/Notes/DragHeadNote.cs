using System;
using System.Linq;

using Cysharp.Threading.Tasks;
using UnityEngine;

public class DragHeadNote : Note
{
    protected override NoteRenderer CreateRenderer()
    {
        return Game.Config.UseClassicStyle
            ? (NoteRenderer) new ClassicDragHeadNoteRenderer(this)
            : throw new NotSupportedException();
    }

    public bool IsCollecting;

    // Drag head is constantly moving from drag note to drag note
    public ChartModel.Note FromNoteModel { get; protected set; }
    public ChartModel.Note ToNoteModel { get; protected set; }
    public ChartModel.Note StartToNoteModel { get; protected set; }
    public ChartModel.Note EndNoteModel { get; protected set; }
    public Vector3 OriginalPosition { get; protected set; }
    
    private bool hasFromNote;
    private Note fromNote;
    private bool hasToNote;
    private Note toNote;

    public bool IsCDrag => Model.type == (int) NoteType.CDragHead;

    public override void SetData(int noteId)
    {
        base.SetData(noteId);
        FromNoteModel = Model;
        ToNoteModel = Model.next_id > 0 ? Chart.note_map[Model.next_id] : Model;
        StartToNoteModel = ToNoteModel;
        EndNoteModel = FromNoteModel.GetDragEndNote(Game.Chart.Model);
    }

    protected override void OnGameUpdate(Game _)
    {
        base.OnGameUpdate(_);
        if (Game.Time < Model.start_time)
        {
            OriginalPosition = transform.localPosition;
        }
    }

    protected override void OnGameLateUpdate(Game _)
    {
        base.OnGameLateUpdate(_);

        transform.localEulerAngles = FromNoteModel.rotation;

        if (Game.SpawnedNotes.ContainsKey(FromNoteModel.id))
        {
            if (!hasFromNote)
            {
                hasFromNote = true;
                fromNote = Game.SpawnedNotes[FromNoteModel.id];
            }
        }
        else
        {
            if (hasFromNote)
            {
                hasFromNote = false;
                fromNote = null;
            }
        }
        if (Game.SpawnedNotes.ContainsKey(ToNoteModel.id))
        {
            if (!hasToNote)
            {
                hasToNote = true;
                toNote = Game.SpawnedNotes[ToNoteModel.id];
            }
        }
        else
        {
            if (hasToNote)
            {
                hasToNote = false;
                toNote = null;
            }
        }

        if (Game.Time >= Model.start_time)
        {
            // Consume zero-span edges in the same frame so same-tick chains do not linger on NaN/u=Inf.
            // Cap iterations against malformed charts with cyclic next_id and non-increasing start_time.
            var maxEdgeSteps = Chart.note_map.Count + 1;
            var edgeSteps = 0;
            while (edgeSteps++ < maxEdgeSteps)
            {
                var fromPos = (hasFromNote && fromNote != this)
                    ? fromNote.StackVisualLocalPosition()
                    : FromNoteModel.CalculatePosition(Game.Chart);
                var toPos = hasToNote
                    ? toNote.StackVisualLocalPosition()
                    : ToNoteModel.CalculatePosition(Game.Chart);

                var span = ToNoteModel.start_time - FromNoteModel.start_time;
                float u;
                if (span > 0f)
                    u = Mathf.Clamp01((Game.Time - FromNoteModel.start_time) / span);
                else
                    u = Game.Time >= ToNoteModel.start_time ? 1f : 0f;

                transform.localPosition = Vector3.Lerp(fromPos, toPos, u);

                if (Game.Time < ToNoteModel.start_time)
                    break;

                if (ToNoteModel == EndNoteModel)
                {
                    transform.localPosition = toPos;
                    break;
                }

                FromNoteModel = ToNoteModel;
                ToNoteModel = Chart.note_map[FromNoteModel.next_id];
                hasFromNote = false;
                hasToNote = false;
                fromNote = null;
                toNote = null;

                if (Game.SpawnedNotes.TryGetValue(FromNoteModel.id, out fromNote))
                    hasFromNote = true;
                if (Game.SpawnedNotes.TryGetValue(ToNoteModel.id, out toNote))
                    hasToNote = true;
            }

            // Moving to or already at last note
            if (ToNoteModel == EndNoteModel)
            {
                // Last note does not exist?
                if (!Game.SpawnedNotes.ContainsKey(ToNoteModel.id))
                {
                    // Clear this
                    if (!IsCleared && ShouldMiss())
                    {
                        Clear(NoteGrade.Miss);
                    }
                    SyncDragStackFollowersIfPrimary();
                    return;
                }

                // Last note does exist and is cleared?
                var lastNote = Game.SpawnedNotes[ToNoteModel.id];
                if (lastNote.IsCleared)
                {
                    // Clear this
                    if (!IsCleared && ShouldMiss())
                    {
                        Clear(NoteGrade.Miss);
                    }
                }
            }
        }
        else
        {
            hasFromNote = false;
            hasToNote = false;
            fromNote = null;
            toNote = null;
            FromNoteModel = Model;
            ToNoteModel = StartToNoteModel;
        }

        SyncDragStackFollowersIfPrimary();
    }

    // SYNC-WARNING: DropDrag judgment is a verbatim copy of DragHeadNote's CanHandleTouch, CalculateGrade,
    // IsAutoEnabled, and PlayHitSound (DragHeadNote.cs:165-252). If you change judgment in either
    // file, update the other. DragHead chain logic (OnGameLateUpdate, Collect, FromNoteModel/ToNoteModel)
    // is INTENTIONALLY NOT inherited — DropDrag is standalone per design decision.
    // NOTE: DragHead's cross-page check is redundant for drop notes (5×pageDuration >> Page.Duration/2
    // always, so the 0.31s check dominates) — safe to copy verbatim, do not remove.
    // Mirror: Assets/Scripts/Game/Notes/Drop/DropDragNote.cs
    public override bool CanHandleTouch()
    {
        if (!base.CanHandleTouch()) return false;
        if (!IsCDrag)
        {
            // Do not handle touch event if touched too ahead of scanner
            if (Model.start_time - Game.Time > 0.31f) return false;
            // Do not handle touch event if in a later page, unless the timing is close (half a screen) TODO: Fix inaccurate algorithm
            if (Model.page_index > Game.Chart.CurrentPageId &&
                Model.start_time - Game.Time > Page.Duration / 2f) return false;
        }
        return true;
    }

    public override async void Collect()
    {
        if (IsCollected) return;

        IsCollecting = true;
        
        void Collect()
        {
            FromNoteModel = default;
            ToNoteModel = default;
            StartToNoteModel = default;
            EndNoteModel = default;
            OriginalPosition = default;
            hasFromNote = default;
            fromNote = default;
            hasToNote = default;
            toNote = default;
        }
        bool CanCollect() => Game.Time >= EndNoteModel.end_time +
            (IsCDrag ? NoteType.CDragChild : NoteType.DragChild).GetDefaultMissThreshold();
        if (CanCollect())
        {
            IsCollecting = false;
            Collect();
            base.Collect();
            return;
        }
        // Don't destroy until the drag is over
        await UniTask.WaitUntil(() => IsCollected || CanCollect());
        
        IsCollecting = false;
        Collect();
        base.Collect();
    }

    public override NoteGrade CalculateGrade()
    {
        if (IsCDrag)
        {
            return base.CalculateGrade();
        }
        var grade = NoteGrade.Miss;
        var timeUntilStart = TimeUntilStart + JudgmentOffset;
        if (timeUntilStart >= 0)
        {
            grade = NoteGrade.None;
            if (timeUntilStart < 0.500f)
            {
                grade = NoteGrade.Perfect;
            }
        }
        else
        {
            var timePassed = -timeUntilStart;
            if (timePassed < 0.200f)
            {
                grade = NoteGrade.Perfect;
            }
        }
        return grade;
    }

    public override bool IsAutoEnabled()
    {
        return base.IsAutoEnabled() || Game.State.Mods.Contains(Mod.AutoDrag);
    }
    
    public override void PlayHitSound()
    {
        if (Context.AudioManager.IsSfxLoaded("HitSound"))
        {
            Context.AudioManager.GetSfx("HitSound").Play();
        }
        Context.Haptic(HapticTypes.Selection, false);
    }
    
}
