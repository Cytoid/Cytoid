using System;
using System.Collections;
using Cytus2.Controllers;
using Cytus2.Models;
using UnityEngine;

namespace Cytus2.Views
{
    public class DragChildNoteView : SimpleNoteView
    {
        protected SpriteMask SpriteMask;
        
        public DragChildNoteView(DragChildNote dragChildNote) : base(dragChildNote)
        {
            SpriteMask = Note.transform.GetComponentInChildren<SpriteMask>();
        }

        public override void OnInit(ChartRoot chart, ChartNote note)
        {
            base.OnInit(chart, note);
            SpriteMask.frontSortingOrder = note.id + 1;
            SpriteMask.backSortingOrder = note.id - 2;
        }

        public override void OnRender()
        {
            base.OnRender();
            Ring.enabled = false;
        }

        protected override void RenderTransform()
        {
            var minSize = 0.7f;
            var timeRequired = 1.175f / Note.Note.speed;
            var timeScaledSize = Size * minSize + Size * (1 - minSize) *
                                 Mathf.Clamp((Game.Time - Note.Note.intro_time) / timeRequired, 0f, 1f);

            Note.transform.localScale = new Vector3(timeScaledSize, timeScaledSize, Note.transform.localScale.z);
        }

        public override void OnLateUpdate()
        {
            base.OnLateUpdate();
            
            if (!Note.IsCleared)
            {
                SpriteMask.enabled = Game.Time >= Note.Note.intro_time;
            }
        }

        protected override void RenderFill()
        {
        }

        public override void OnClear(NoteGrade grade)
        {
            base.OnClear(grade);

            if (!(Game is StoryboardGame))
            {
                Fill.enabled = true; // Still render it
                SpriteMask.enabled = false;
            }
        }
    }
}