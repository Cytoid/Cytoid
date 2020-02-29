using System;
using System.Linq;
using UniRx.Async;
using UnityEngine;

public class DragHeadNote : Note
{
    protected override NoteRenderer CreateRenderer()
    {
        return Game.Config.UseClassicStyle
            ? (NoteRenderer) new DragHeadClassicNoteRenderer(this)
            : new DragHeadDefaultNoteRenderer(this);
    }

    // Drag head is constantly moving from drag note to drag note
    public ChartModel.Note FromNoteModel { get; protected set; }
    public ChartModel.Note ToNoteModel { get; protected set; }
    public ChartModel.Note EndNoteModel { get; protected set; }

    public override void SetData(Game game, int noteId)
    {
        base.SetData(game, noteId);
        FromNoteModel = Model;
        ToNoteModel = Model.next_id > 0 ? Chart.note_map[Model.next_id] : Model;
        EndNoteModel = FromNoteModel.GetDragEndNote(game.Chart.Model);
    }

    protected override void OnGameUpdate()
    {
        base.OnGameUpdate();

        if (Game.Time >= Model.start_time)
        {
            // Move drag head
            transform.position = Vector3.Lerp(FromNoteModel.position, ToNoteModel.position,
                (Game.Time - FromNoteModel.start_time) / (ToNoteModel.start_time - FromNoteModel.start_time));
            transform.eulerAngles = new Vector3(0, 0, 45 - ToNoteModel.rotation);
            
            // Moved to next note?
            if (Game.Time >= ToNoteModel.start_time)
            {
                if (ToNoteModel == EndNoteModel) // Last note
                {
                    transform.position = ToNoteModel.position;
                }
                else
                {
                    FromNoteModel = ToNoteModel;
                    ToNoteModel = Chart.note_map[FromNoteModel.next_id];
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
            gameObject.transform.position = Model.position;
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
        await UniTask.WaitUntil(() => Game.Time >= EndNoteModel.end_time + NoteType.DragChild.GetDefaultMissThreshold());
        Destroy();
    }

    public override NoteGrade CalculateGrade()
    {
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