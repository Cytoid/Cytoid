using System;
using System.Collections;
using Cytus2.Controllers;
using Cytus2.Models;
using UnityEngine;

namespace Cytus2.Views
{

    public class DragChildNoteView : SimpleNoteView
    {

        public DragChildNoteView(DragChildNote dragChildNote) : base(dragChildNote)
        {
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
            var timeScaledSize = Size * minSize + Size * (1 - minSize) * Mathf.Clamp((Game.Time - Note.Note.intro_time) / timeRequired, 0f, 1f);
            
            Note.transform.localScale = new Vector3(timeScaledSize, timeScaledSize, Note.transform.localScale.z);
        }

        protected override void RenderFill()
        {
        }

        public override void OnClear(NoteGrading grading)
        {
            base.OnClear(grading);

            if (!(Game is StoryboardGame)) {
                Fill.enabled = true; // Still render it
            }
        }

    }

}