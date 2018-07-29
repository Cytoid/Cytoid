using System.Collections;
using Cytus2.Controllers;
using Cytus2.Models;
using UnityEngine;

namespace Cytus2.Views
{
    public class HoldNoteView : SimpleNoteView
    {
        public new HoldNote Note;

        public SpriteRenderer Line;
        public SpriteRenderer CompletedLine;
        public ProgressRingView ProgressRing;
        public TriangleView Triangle;

        protected SpriteMask SpriteMask;
        protected int TicksUntilHoldFx;
        protected const int MaxTicksBetweenHoldFx = 9;

        private bool playedEarlyHitSound;

        public HoldNoteView(HoldNote holdNote) : base(holdNote)
        {
            Note = holdNote;
            InitViews();
        }

        protected virtual void InitViews()
        {
            var provider = HoldNoteViewProvider.Instance;
            Line = Object.Instantiate(provider.LinePrefab, Note.transform, false).GetComponent<SpriteRenderer>();
            Line.color = Line.color.WithAlpha(0);
            CompletedLine = Object.Instantiate(provider.CompletedLinePrefab, Note.transform, false)
                .GetComponent<SpriteRenderer>();
            ProgressRing = Object.Instantiate(provider.ProgressRingPrefab, Note.transform, false)
                .GetComponent<ProgressRingView>();
            Triangle = Object.Instantiate(provider.TrianglePrefab).GetComponent<TriangleView>();
            ProgressRing.MaxCutoff = 0;
            ProgressRing.FillCutoff = 0;
            ProgressRing.gameObject.GetComponent<SpriteRenderer>().material.renderQueue =
                3000 + Note.Note.id; // TODO: 3000?
            CompletedLine.size = new Vector2(1, 0);

            SpriteMask = Note.transform.GetComponentInChildren<SpriteMask>();
        }

        public override void OnInit(ChartRoot chart, ChartNote note)
        {
            base.OnInit(chart, note);
            Triangle.Note = note;
            TicksUntilHoldFx = MaxTicksBetweenHoldFx;
        }

        public override void OnRender()
        {
            if (!IsRendered())
            {
                Line.enabled = true;
                CompletedLine.enabled = true;
                CompletedLine.size = new Vector2(1, 0);
                ProgressRing.enabled = true;
                Triangle.enabled = true;
                SpriteMask.enabled = true;
                if (Mod.HideNotes.IsEnabled())
                {
                    Line.enabled = false;
                    CompletedLine.enabled = false;
                    Triangle.enabled = false;
                }
            }

            base.OnRender();
        }

        protected override void RenderOpacity()
        {
            base.RenderOpacity();
            Line.color = Line.color.WithAlpha(EasedOpacity);
        }

        public void OnStartHolding()
        {
        }

        public void OnStopHolding()
        {
        }

        public override void OnLateUpdate()
        {
            if (Note.IsHolding && Game.Time >= Note.Note.start_time)
            {
                Triangle.IsShowing = true;
            }
            else
            {
                Triangle.IsShowing = false;
            }

            if (!Note.IsCleared)
            {
                Line.flipY = Note.Note.direction == -1;
                CompletedLine.flipY = Line.flipY;
                CompletedLine.color = Fill.color;
                Line.sortingOrder = Ring.sortingOrder;
                CompletedLine.sortingOrder = Ring.sortingOrder + 1;
                SpriteMask.frontSortingOrder = CompletedLine.sortingOrder + 1;
                SpriteMask.backSortingOrder = Line.sortingOrder - 1;
               
                SpriteMask.enabled = Game.Time >= Note.Note.intro_time;

                if (Note.IsHolding)
                {
                    if (Note.Game.Time > Note.Note.start_time)
                    {
                        if (!playedEarlyHitSound && PlayerPrefsExt.GetBool("early hit sounds"))
                        {
                            playedEarlyHitSound = true;
                            
                            Note.PlayHitSound();
                        }
                        
                        ProgressRing.FillColor = Fill.color;
                        ProgressRing.MaxCutoff = Mathf.Min(1, 1.333f * Note.Progress);
                        ProgressRing.FillCutoff = Mathf.Min(1, Note.Progress);
                        CompletedLine.size = new Vector2(1, Note.Note.holdlength * Note.Progress);

                        if (TicksUntilHoldFx == MaxTicksBetweenHoldFx)
                        {
                            TicksUntilHoldFx = 0;
                            SimpleEffects.Instance.PlayHoldFx(this);
                        }

                        TicksUntilHoldFx++;
                    }
                }
            }
        }

        protected override void RenderTransform()
        {
            // Scale whole transform

            var minPercentageSize = 0.4f;
            var timeRequired = 1.367f / Note.Note.speed;
            var timeScaledSize = Size * minPercentageSize + Size * (1 - minPercentageSize) *
                                 Mathf.Clamp((Game.Time - Note.Note.intro_time) / timeRequired, 0f, 1f);
            var minPercentageLineSize = 0.0f;
            var timeScaledLineSize = minPercentageLineSize + (1 - minPercentageLineSize) *
                                 Mathf.Clamp((Game.Time - Note.Note.intro_time) / timeRequired, 0f, 1f);
            var timeScaledLineSizeY = Mathf.Clamp((Game.Time - Note.Note.intro_time) * 2 / timeRequired, 0f, 1f);
            
            Ring.transform.localScale = new Vector3(timeScaledSize, timeScaledSize, Ring.transform.localScale.z);
            Fill.transform.localScale = new Vector3(timeScaledSize, timeScaledSize, Fill.transform.localScale.z);
            SpriteMask.transform.localScale = new Vector3(timeScaledSize, timeScaledSize, SpriteMask.transform.localScale.z);

            Line.transform.localScale = new Vector2(timeScaledLineSize, Line.transform.localScale.y);
            Line.size = new Vector2(1, 0.21f * Mathf.Floor(Note.Note.holdlength / 0.21f) /* * timeScaledLineSizeY */);
        }

        protected override void RenderFill()
        {
        }

        public override void OnClear(NoteGrade grade)
        {
            base.OnClear(grade);
            Line.enabled = false;
            CompletedLine.enabled = false;
            ProgressRing.Reset();
            ProgressRing.enabled = false;
            Triangle.Reset();
            Triangle.enabled = false;
            SpriteMask.enabled = false;

            if (!(Game is StoryboardGame))
            {
                Object.Destroy(Line.gameObject);
                Object.Destroy(CompletedLine.gameObject);
                Object.Destroy(ProgressRing.gameObject);
                Object.Destroy(Triangle.gameObject);
                Object.Destroy(SpriteMask.gameObject);
            }
        }
    }
}