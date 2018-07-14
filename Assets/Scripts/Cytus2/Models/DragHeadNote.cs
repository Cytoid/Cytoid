using System.Collections;
using Cytus2.Controllers;
using Cytus2.Views;
using UnityEngine;

namespace Cytus2.Models
{
    public class DragHeadNote : GameNote
    {
        public bool IsMoving { get; private set; }

        // Drag head is constantly moving from drag note to drag note
        public ChartNote FromNote;
        public ChartNote ToNote;

        protected override void Awake()
        {
            base.Awake();
            IsMoving = false;
            View = new DragHeadNoteView(this);
            MaxMissThreshold = 0.300f;
        }

        private void Start()
        {
            FromNote = Note;
            ToNote = Chart.note_list[Note.next_id];
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (!View.IsRendered())
            {
                View.OnRender();
            }

            var time = Game.Time;
            IsMoving = time >= Note.start_time;

            if (IsMoving)
            {
                if (time >= ToNote.start_time)
                {
                    gameObject.transform.eulerAngles = new Vector3(0, 0, 45 - ToNote.rotation);
                    if (ToNote.next_id <= 0)
                    {
                        IsMoving = false;
                        gameObject.transform.position = ToNote.position;
                    }
                    else
                    {
                        FromNote = ToNote;
                        ToNote = Chart.note_list[FromNote.next_id];
                    }
                }

                if (IsMoving)
                {
                    gameObject.transform.position = Vector3.Lerp(FromNote.position, ToNote.position,
                        (time - FromNote.start_time) / (ToNote.start_time - FromNote.start_time));
                }
            }
            else
            {
                gameObject.transform.position = Note.position;
                FromNote = Note;
                ToNote = Chart.note_list[Note.next_id];
            }
        }

        public override void Touch(Vector2 screenPos)
        {
            // Do not handle touch event if touched too ahead of scanner
            if (Note.start_time - Game.Time > 0.31f) return;
            // Do not handle touch event if in a later page, unless the timing is close (half a screen) TODO: Fix inaccurate algorithm
            if (Note.page_index > Game.CurrentPageId && Note.start_time - Game.Time > Page.Duration / 2f) return;
            base.Touch(screenPos);
        }

        protected override IEnumerator DestroyLater()
        {
            // Do nothing; drag head should be destroyed when last drag note is cleared
            yield return null;
        }

        public override NoteGrading CalculateGrading()
        {
            var ranking = NoteGrading.Miss;
            if (TimeUntilStart >= 0)
            {
                ranking = NoteGrading.Undetermined;
                if (TimeUntilStart < 0.500f)
                {
                    ranking = NoteGrading.Perfect;
                }
            }
            else
            {
                var timePassed = -TimeUntilStart;
                if (timePassed < 0.200f)
                {
                    ranking = NoteGrading.Perfect;
                }
            }

            return ranking;
        }
    }
}