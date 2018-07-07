using System;
using System.Collections;
using Cytus2.Controllers;
using Cytus2.Models;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Cytus2.Views
{
    public class DragHeadNoteView : SimpleNoteView
    {
        private SpriteMask spriteMask;

        public DragHeadNoteView(DragHeadNote dragHeadNote) : base(dragHeadNote)
        {
            spriteMask = Note.transform.GetComponentInChildren<SpriteMask>();
        }

        public override void OnInit(ChartRoot chart, ChartNote note)
        {
            base.OnInit(chart, note);
            Fill.sortingOrder = Ring.sortingOrder + 1;
            spriteMask.frontSortingOrder = note.id + 1;
            spriteMask.backSortingOrder = note.id - 2;
        }

        public override void OnRender()
        {
            if (!IsRendered())
            {
                Ring.enabled = true;
                Fill.enabled = true;
            }
            base.OnRender();
        }

        public override void OnLateUpdate()
        {
            base.OnLateUpdate();
           
            DragHeadNote note = (DragHeadNote) Note;

            spriteMask.enabled = Game.Time >= Note.Note.intro_time;
            spriteMask.isCustomRangeActive = spriteMask.enabled;
            
            if (Game.Time >= note.Note.start_time)
            {
                if (note.ToNote.next_id <= 0)
                {
                    if (!(Game is StoryboardGame))
                    {
                        if (!note.Game.GameNotes.ContainsKey(note.ToNote.id))
                        {
                            Object.Destroy(note.gameObject);
                            return;
                        }
                    }

                    var lastNote = note.Game.GameNotes[note.ToNote.id];

                    if (lastNote.IsCleared)
                    {
                        if (Game is StoryboardGame)
                        {
                            Ring.enabled = false;
                            Fill.enabled = false;
                        }
                        else
                        {
                            Object.Destroy(note.gameObject);
                        }
                    }
                }
            }
        }

        protected override void RenderTransform()
        {
            var minSize = 0.7f;
            var timeRequired = 1.175f / Note.Note.speed;
            var timeScaledSize = Size * minSize + Size * (1 - minSize) * Mathf.Clamp((Game.Time - Note.Note.intro_time) / timeRequired, 0f, 1f);
            
            Note.transform.localScale = new Vector3(timeScaledSize, timeScaledSize, Note.transform.localScale.z);
        }

        protected override void RenderFill()
        {
        }

        public override void OnClear(NoteGrading grading)
        {
            base.OnClear(grading);
            
            // They still display
            Ring.enabled = true;
            Fill.enabled = true;
        }
        
    }
}