using Cytus2.Controllers;
using Cytus2.Models;
using DG.Tweening;
using UnityEngine;

namespace Cytus2.Views
{
    public class SimpleNoteView : NoteView
    {
        
        public readonly Game Game;

        protected float Size;
        private bool rendered;

        public readonly SpriteRenderer Ring;
        public readonly SpriteRenderer Fill;

        public SimpleNoteView(GameNote note) : base(note)
        {
            Game = note.Game;

            Ring = Note.transform.Find("NoteRing").GetComponent<SpriteRenderer>();
            Fill = Note.transform.Find("NoteFill").GetComponent<SpriteRenderer>();

            Ring.enabled = false;
            Fill.enabled = false;
        }

        public override void OnInit(ChartRoot chart, ChartNote note)
        {
            Size = SimpleVisualOptions.Instance.GetSize(this);

            Ring.sortingOrder = (chart.note_list.Count - note.id) * 3;
            Fill.sortingOrder = Ring.sortingOrder - 1;
        }

        public override void OnRender()
        {
            if (!rendered)
            {
                rendered = true;
                Ring.enabled = true;
                Fill.enabled = true;
                Collider.enabled = true;

                if (Mod.HideNotes.IsEnabled())
                {
                    Ring.enabled = false;
                    Fill.enabled = false;
                }
            }

            RenderComponents();
        }

        public virtual void RenderComponents()
        {
            Ring.color = SimpleVisualOptions.Instance.GetRingColor(this);
            Fill.color = SimpleVisualOptions.Instance.GetFillColor(this);

            RenderTransform();
            RenderFill();
            RenderOpacity();
        }

        protected virtual void RenderTransform()
        {
            // Scale whole transform

            var minPercentageSize = 0.4f;
            var timeRequired = 1.367f / Note.Note.speed;
            var timeScaledSize = Size * minPercentageSize + Size * (1 - minPercentageSize) *
                                 Mathf.Clamp((Game.Time - Note.Note.intro_time) / timeRequired, 0f, 1f);
            
            Note.transform.localScale = new Vector3(timeScaledSize, timeScaledSize, Note.transform.localScale.z);
        }

        protected virtual void RenderFill()
        {
            // Scale fill

            float t;
            if (Note.TimeUntilStart > 0)
                t = Mathf.Clamp((Game.Time - Note.Note.intro_time) / (Note.Note.start_time - Note.Note.intro_time), 0f,
                    1f);
            else t = 1f;

            var z = Fill.transform.localScale.z;
            Fill.transform.localScale = Vector3.Lerp(new Vector3(0, 0, z), new Vector3(1, 1, z), t);
        }

        protected float EasedOpacity;

        protected virtual void RenderOpacity()
        {
            var maxOpacity = (float) Game.Chart.Root.opacity;
            if (Note.Note.opacity != double.MinValue)
            {
                maxOpacity = (float) Note.Note.opacity;
            }

            if (Note.TimeUntilStart > 0)
                EasedOpacity =
                    Mathf.Clamp((Game.Time - Note.Note.intro_time) / (Note.Note.start_time - Note.Note.intro_time) * 2f,
                        0f, maxOpacity);
            else EasedOpacity = maxOpacity;

            EasedOpacity *= SimpleVisualOptions.Instance.GlobalOpacityMultiplier;

            Ring.color = Ring.color.WithAlpha(EasedOpacity);
            Fill.color = Fill.color.WithAlpha(EasedOpacity);
        }

        public override void OnClear(NoteGrade grade)
        {
            SimpleEffects.Instance.PlayClearFx(
                this,
                grade,
                Note.TimeUntilEnd,
                GameOptions.Instance.ShowEarlyLateIndicator
            );

            rendered = false;

            if (Ring != null)
            {
                Ring.enabled = false;
                Fill.enabled = false;
                Collider.enabled = false;
            }
        }

        public override bool IsRendered()
        {
            return rendered;
        }
    }
}