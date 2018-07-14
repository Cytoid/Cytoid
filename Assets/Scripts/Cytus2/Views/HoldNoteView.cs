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

        protected SpriteRenderer Mask;
        protected SpriteMask SpriteMask;
        protected int TicksUntilHoldFx;
        protected const int MaxTicksBetweenHoldFx = 9;

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

            Mask = Note.transform.Find("Mask").GetComponent<SpriteRenderer>();
            Mask.enabled = false;
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
                Mask.enabled = true;
                SpriteMask.enabled = true;
                if (Mod.HideNotes.IsEnabled())
                {
                    Line.enabled = false;
                    CompletedLine.enabled = false;
                    Triangle.enabled = false;
                }
            }

            base.OnRender();

            Mask.color = Fill.color.WithAlpha(0.35f);
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
                Mask.enabled = Note.IsHolding;
                Line.flipY = Note.Note.direction == -1;
                CompletedLine.flipY = Line.flipY;
                Line.size = new Vector2(1, 0.21f * Mathf.Floor(Note.Note.holdlength / 0.21f));
                CompletedLine.color = Fill.color;
                Line.sortingOrder = Ring.sortingOrder;
                CompletedLine.sortingOrder = Ring.sortingOrder + 1;
                SpriteMask.frontSortingOrder = Line.sortingOrder;
                SpriteMask.backSortingOrder = Line.sortingOrder - 1;
                if (Note.Game.Time < Note.Note.start_time)
                {
                    SpriteMask.isCustomRangeActive = false;
                }

                if (Note.IsHolding)
                {
                    if (Note.Game.Time > Note.Note.start_time)
                    {
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
            Ring.transform.localScale = new Vector3(Size, Size, Ring.transform.localScale.z);
            Fill.transform.localScale = new Vector3(Size, Size, Fill.transform.localScale.z);
        }

        protected override void RenderFill()
        {
        }

        public override void OnClear(NoteGrading grading)
        {
            base.OnClear(grading);
            Line.enabled = false;
            CompletedLine.enabled = false;
            ProgressRing.Reset();
            ProgressRing.enabled = false;
            Triangle.Reset();
            Triangle.enabled = false;
            Mask.enabled = false;
            SpriteMask.enabled = false;

            if (!(Game is StoryboardGame))
            {
                Object.Destroy(Line.gameObject);
                Object.Destroy(CompletedLine.gameObject);
                Object.Destroy(ProgressRing.gameObject);
                Object.Destroy(Triangle.gameObject);
                Object.Destroy(Mask.gameObject);
                Object.Destroy(SpriteMask.gameObject);
            }
        }
    }
}