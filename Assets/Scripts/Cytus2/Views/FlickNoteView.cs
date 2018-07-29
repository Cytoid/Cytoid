using System;
using System.Collections;
using Cytus2.Models;
using UnityEngine;

namespace Cytus2.Views
{
    public class FlickNoteView : SimpleNoteView
    {
        private SpriteRenderer leftArrow;
        private SpriteRenderer rightArrow;

        private static float MaxArrowOffset = Camera.main.orthographicSize * 0.3f;

        public FlickNoteView(FlickNote flickNote) : base(flickNote)
        {
            leftArrow = Note.transform.Find("LeftArrow").GetComponent<SpriteRenderer>();
            rightArrow = Note.transform.Find("RightArrow").GetComponent<SpriteRenderer>();
            leftArrow.transform.ChangeLocalPosition(x: -MaxArrowOffset);
            rightArrow.transform.ChangeLocalPosition(x: MaxArrowOffset);
            leftArrow.color = leftArrow.color.WithAlpha(0);
            rightArrow.color = rightArrow.color.WithAlpha(0);
        }

        public override void OnClear(NoteGrade grade)
        {
            base.OnClear(grade);
            leftArrow.enabled = false;
            rightArrow.enabled = false;
        }

        public override void OnRender()
        {
            if (!IsRendered())
            {
                leftArrow.enabled = true;
                rightArrow.enabled = true;
                if (Mod.HideNotes.IsEnabled())
                {
                    leftArrow.enabled = false;
                    rightArrow.enabled = false;
                }
            }

            base.OnRender();
        }

        public override void RenderComponents()
        {
            base.RenderComponents();
            RenderArrows();
        }

        protected override void RenderOpacity()
        {
            base.RenderOpacity();
            leftArrow.color = leftArrow.color.WithAlpha(EasedOpacity);
            rightArrow.color = rightArrow.color.WithAlpha(EasedOpacity);
        }

        protected virtual void RenderArrows()
        {
            leftArrow.transform.localPosition = Vector3.Lerp(
                new Vector3(-MaxArrowOffset, 0, 0),
                new Vector3(0, 0, 0),
                Mathf.Clamp((Game.Time - Note.Note.intro_time) / (Note.Note.start_time - Note.Note.intro_time - 0.25f),
                    0, 1)
            );
            rightArrow.transform.localPosition = Vector3.Lerp(
                new Vector3(MaxArrowOffset, 0, 0),
                new Vector3(0, 0, 0),
                Mathf.Clamp((Game.Time - Note.Note.intro_time) / (Note.Note.start_time - Note.Note.intro_time - 0.25f),
                    0, 1)
            );
        }
    }
}