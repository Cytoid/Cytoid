using System.Collections;
using Cytus2.Controllers;
using Cytus2.Models;
using UnityEngine;

namespace Cytus2.Views
{
    public class LongHoldNoteView : HoldNoteView
    {
        public SpriteRenderer Line2;
        public SpriteRenderer CompletedLine2;

        public LongHoldNoteView(LongHoldNote holdNote) : base(holdNote)
        {
        }

        protected override void InitViews()
        {
            // Override base view

            var provider = LongHoldNoteViewProvider.Instance;
            Line = Object.Instantiate(provider.LinePrefab, Note.transform, false).GetComponent<SpriteRenderer>();
            Line.color = Line.color.WithAlpha(0);
            Line2 = Object.Instantiate(provider.LinePrefab, Note.transform, false).GetComponent<SpriteRenderer>();
            Line2.color = Line2.color.WithAlpha(0);
            CompletedLine = Object.Instantiate(provider.CompletedLinePrefab, Note.transform, false)
                .GetComponent<SpriteRenderer>();
            CompletedLine2 = Object.Instantiate(provider.CompletedLinePrefab, Note.transform, false)
                .GetComponent<SpriteRenderer>();
            ProgressRing = Object.Instantiate(provider.ProgressRingPrefab, Note.transform, false)
                .GetComponent<ProgressRingView>();
            Triangle = Object.Instantiate(provider.TrianglePrefab).GetComponent<TriangleView>();
            ProgressRing.MaxCutoff = 0;
            ProgressRing.FillCutoff = 0;
            ProgressRing.gameObject.GetComponent<SpriteRenderer>().material.renderQueue =
                3000 + Note.Note.id; // TODO: 3000?
            CompletedLine.size = new Vector2(1, 0);
            CompletedLine2.size = new Vector2(1, 0);
            Mask = Note.transform.Find("Mask").GetComponent<SpriteRenderer>();
            Mask.enabled = false;
            SpriteMask = Note.transform.GetComponentInChildren<SpriteMask>();
        }

        public override void OnRender()
        {
            if (!IsRendered())
            {
                Line2.enabled = true;
                CompletedLine2.enabled = true;
                CompletedLine2.size = new Vector2(1, 0);

                if (Mod.HideNotes.IsEnabled())
                {
                    Line2.enabled = false;
                    CompletedLine2.enabled = false;
                }
            }

            base.OnRender();
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
                Line2.flipY = !Line.flipY;
                CompletedLine.flipY = Line.flipY;
                CompletedLine2.flipY = !CompletedLine.flipY;
                Line.size = new Vector2(1, Camera.main.orthographicSize * 2);
                Line2.size = new Vector2(1, Camera.main.orthographicSize * 2);
                CompletedLine.color = Fill.color;
                CompletedLine2.color = Fill.color;
                Line.sortingOrder = Ring.sortingOrder;
                Line2.sortingOrder = Ring.sortingOrder;
                CompletedLine.sortingOrder = Ring.sortingOrder + 1;
                CompletedLine2.sortingOrder = Ring.sortingOrder + 1;
                SpriteMask.frontSortingOrder = Line.sortingOrder;
                SpriteMask.backSortingOrder = Line.sortingOrder - 1;
                if (Note.Game.Time > Note.Note.start_time)
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

                        SpriteRenderer topLine, bottomLine;
                        if (CompletedLine.flipY)
                        {
                            bottomLine = CompletedLine;
                            topLine = CompletedLine2;
                        }
                        else
                        {
                            topLine = CompletedLine;
                            bottomLine = CompletedLine2;
                        }

                        topLine.size = new Vector2(1,
                            (Camera.main.orthographicSize - Note.transform.position.y) * Note.Progress);
                        bottomLine.size = new Vector2(1,
                            -(-Camera.main.orthographicSize - Note.transform.position.y) * Note.Progress);

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

        protected override void RenderOpacity()
        {
            base.RenderOpacity();
            Line2.color = Line2.color.WithAlpha(EasedOpacity);
        }

        public override void OnClear(NoteGrading grading)
        {
            base.OnClear(grading);
            Line2.enabled = false;
            CompletedLine2.enabled = false;

            if (!(Game is StoryboardGame))
            {
                Object.Destroy(Line2.gameObject);
                Object.Destroy(CompletedLine2.gameObject);
            }
        }
    }
}