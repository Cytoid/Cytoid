using System;
using System.Linq;
using UniRx.Async;
using UnityEngine;

public class DragHeadNote : Note
{
    protected override NoteRenderer CreateRenderer()
    {
        return Game.Config.UseClassicStyle
            ? (NoteRenderer) new ClassicDragHeadNoteRenderer(this)
            : new DefaultDragHeadNoteRenderer(this);
    }

    // Drag head is constantly moving from drag note to drag note
    public ChartModel.Note FromNoteModel { get; protected set; }
    public ChartModel.Note ToNoteModel { get; protected set; }
    public ChartModel.Note StartToNoteModel { get; protected set; }
    public ChartModel.Note EndNoteModel { get; protected set; }

    private bool hasFromNote;
    private Note fromNote;
    private bool hasToNote;
    private Note toNote;

    public bool IsCDrag => Model.type == (int) NoteType.CDragHead;

    public override void SetData(Game game, int noteId)
    {
        base.SetData(game, noteId);
        FromNoteModel = Model;
        ToNoteModel = Model.next_id > 0 ? Chart.note_map[Model.next_id] : Model;
        StartToNoteModel = ToNoteModel;
        EndNoteModel = FromNoteModel.GetDragEndNote(game.Chart.Model);
    }

    protected override void OnGameLateUpdate()
    {
        base.OnGameLateUpdate();
        
        transform.localEulerAngles = FromNoteModel.rotation;

        if (!hasFromNote && Game.Notes.ContainsKey(FromNoteModel.id))
        {
            hasFromNote = true;
            fromNote = Game.Notes[FromNoteModel.id];
        }
        if (!hasToNote && Game.Notes.ContainsKey(ToNoteModel.id))
        {
            hasToNote = true;
            toNote = Game.Notes[ToNoteModel.id];
        }

        if (Game.Time >= Model.start_time)
        {
            // Move drag head
            transform.localPosition = Vector3.Lerp(
                hasFromNote ? fromNote.transform.localPosition : FromNoteModel.position, 
                hasToNote ? toNote.transform.localPosition : ToNoteModel.position,
                (Game.Time - FromNoteModel.start_time) / (ToNoteModel.start_time - FromNoteModel.start_time));

            // Moved to next note?
            if (Game.Time >= ToNoteModel.start_time)
            {
                if (ToNoteModel == EndNoteModel) // Last note
                {
                    transform.localPosition = hasToNote ? toNote.transform.localPosition : ToNoteModel.position;
                }
                else
                {
                    FromNoteModel = ToNoteModel;
                    ToNoteModel = Chart.note_map[FromNoteModel.next_id];
                    
                    hasFromNote = false;
                    hasToNote = false;
                    fromNote = null;
                    toNote = null;
                }
            }

            // Moving to or already at last note
            if (ToNoteModel == EndNoteModel)
            {
                // Last note does not exist?
                if (!Game.Notes.ContainsKey(ToNoteModel.id))
                {
                    // Clear this
                    if (!IsCleared && ShouldMiss())
                    {
                        Clear(NoteGrade.Miss);
                    }
                    return;
                }

                // Last note does exist and is cleared?
                var lastNote = Game.Notes[ToNoteModel.id];
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
    }

    public override void OnTouch(Vector2 screenPos)
    {
        // Do not handle touch event if touched too ahead of scanner
        if (Model.start_time - Game.Time > 0.31f) return;
        // Do not handle touch event if in a later page, unless the timing is close (half a screen) TODO: Fix inaccurate algorithm
        if (Model.page_index > Game.Chart.CurrentPageId && Model.start_time - Game.Time > Page.Duration / 2f) return;
        base.OnTouch(screenPos);
    }

    protected override async void AwaitAndDestroy()
    {
        // Don't destroy until the drag is over
        await UniTask.WaitUntil(() => Game.Time >= EndNoteModel.end_time + (IsCDrag ? NoteType.CDragChild : NoteType.DragChild).GetDefaultMissThreshold());
        Destroy();
    }

    public override NoteGrade CalculateGrade()
    {
        if (IsCDrag)
        {
            return base.CalculateGrade();
        }
        var grade = NoteGrade.Miss;
        if (TimeUntilStart >= 0)
        {
            grade = NoteGrade.None;
            if (TimeUntilStart < 0.500f)
            {
                grade = NoteGrade.Perfect;
            }
        }
        else
        {
            var timePassed = -TimeUntilStart;
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
    
}