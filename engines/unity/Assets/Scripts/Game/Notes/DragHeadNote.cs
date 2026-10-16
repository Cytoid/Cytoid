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
    
    public bool IsCDrag => Model.type == (int) NoteType.CDragHead;

    public override void SetData(int noteId)
    {
        base.SetData(noteId);
        FromNoteModel = Model;
        ToNoteModel = Model.next_id > 0 ? Chart.note_map[Model.next_id] : Model;
        StartToNoteModel = ToNoteModel;
        EndNoteModel = FromNoteModel.GetDragEndNote(Game.Chart.Model);
    }

    public override void PreparePosition()
    {
        if (IsDragStackFollower) return;
        OriginalPosition = Game.GetDragPathPosition(Model);
        if (Game.Time < Model.start_time)
        {
            FromNoteModel = Model;
            ToNoteModel = StartToNoteModel;
            transform.localPosition = Model.CalculatePosition(Game.Chart);
            return;
        }

        // Advance all crossed edges in this frame, including simultaneous nodes.
        // Endpoints never depend on a spawned/cleared/recycled Note instance.
        var steps = 0;
        while (Game.Time >= ToNoteModel.start_time && ToNoteModel != EndNoteModel &&
               steps++ <= Chart.note_map.Count)
        {
            FromNoteModel = ToNoteModel;
            ToNoteModel = Chart.note_map[FromNoteModel.next_id];
        }
        transform.localPosition = DragPathGeometry.HeadPosition(
            Game.GetDragPathPosition(FromNoteModel), Game.GetDragPathPosition(ToNoteModel),
            Game.Time, FromNoteModel.start_time, ToNoteModel.start_time);
    }

    protected override void OnGameLateUpdate(Game _)
    {
        var approaching = Game.Time < Model.start_time;
        var from = approaching ? transform.localPosition : Game.GetDragPathPosition(FromNoteModel);
        var to = approaching ? ToNoteModel.CalculatePosition(Game.Chart) : Game.GetDragPathPosition(ToNoteModel);
        transform.localEulerAngles = Model.rotation =
            DragPathGeometry.Rotation(from, to, FromNoteModel.Override);

        if (Game.Time >= Model.start_time && ToNoteModel == EndNoteModel)
        {
            if (!Game.SpawnedNotes.TryGetValue(ToNoteModel.id, out var last) || last.IsCleared)
            {
                if (!IsCleared && ShouldMiss()) Clear(NoteGrade.Miss);
            }
        }
        SyncDragStackFollowersIfPrimary();
    }

    // SYNC-WARNING: DropDrag judgment is a verbatim copy of DragHeadNote's CanHandleTouch, CalculateGrade,
    // IsAutoEnabled, and PlayHitSound. If you change judgment in either
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
