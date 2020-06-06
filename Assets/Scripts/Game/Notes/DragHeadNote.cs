using System;
using System.Linq;
using MoreMountains.NiceVibrations;
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
            // Move drag head
            transform.localPosition = Vector3.Lerp(
                (hasFromNote && fromNote != this) ? fromNote.transform.localPosition : FromNoteModel.position, 
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
                if (!Game.SpawnedNotes.ContainsKey(ToNoteModel.id))
                {
                    // Clear this
                    if (!IsCleared && ShouldMiss())
                    {
                        Clear(NoteGrade.Miss);
                    }
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
    }

    public override void OnTouch(Vector2 screenPos)
    {
        if (!IsCDrag)
        {
            // Do not handle touch event if touched too ahead of scanner
            if (Model.start_time - Game.Time > 0.31f) return;
            // Do not handle touch event if in a later page, unless the timing is close (half a screen) TODO: Fix inaccurate algorithm
            if (Model.page_index > Game.Chart.CurrentPageId &&
                Model.start_time - Game.Time > Page.Duration / 2f) return;
        }
        base.OnTouch(screenPos);
    }

    public override async void Collect()
    {
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
            Collect();
            base.Collect();
            return;
        }
        // Don't destroy until the drag is over
        await UniTask.WaitUntil(CanCollect);
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
        if (Context.AudioManager.IsLoaded("HitSound"))
        {
            Context.AudioManager.Get("HitSound").Play();
        }
        Context.Haptic(HapticTypes.Selection, false);
    }
    
}