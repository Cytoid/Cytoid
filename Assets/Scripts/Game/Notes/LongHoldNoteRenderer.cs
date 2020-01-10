using System.Linq.Expressions;
using UnityEngine;

public class LongHoldNoteRenderer : HoldNoteRenderer
{
    public SpriteRenderer Line2;
    public SpriteRenderer CompletedLine2;

    private float orthographicSize = Camera.main.orthographicSize;

    public LongHoldNoteRenderer(LongHoldNote holdNote) : base(holdNote) => Expression.Empty();

    protected override void InitializeHoldComponents()
    {
        // Override base renderer
        var provider = LongHoldNoteRendererProvider.Instance;
        Line = Object.Instantiate(provider.linePrefab, Note.transform, false).GetComponent<SpriteRenderer>();
        Line.color = Line.color.WithAlpha(0);
        Line2 = Object.Instantiate(provider.linePrefab, Note.transform, false).GetComponent<SpriteRenderer>();
        Line2.color = Line2.color.WithAlpha(0);
        CompletedLine = Object.Instantiate(provider.completedLinePrefab, Note.transform, false)
            .GetComponent<SpriteRenderer>();
        CompletedLine2 = Object.Instantiate(provider.completedLinePrefab, Note.transform, false)
            .GetComponent<SpriteRenderer>();
        ProgressRing = Object.Instantiate(provider.progressRingPrefab, Note.transform, false)
            .GetComponent<ProgressRing>();
        Triangle = Object.Instantiate(provider.trianglePrefab).GetComponent<MeshTriangle>();
        var newProgressRingScale = ProgressRing.transform.localScale.x * SizeMultiplier;
        ProgressRing.transform.SetLocalScaleXY(newProgressRingScale, newProgressRingScale);
        ProgressRing.maxCutoff = 0;
        ProgressRing.fillCutoff = 0;
        ProgressRing.gameObject.GetComponent<SpriteRenderer>().material.renderQueue =
            3000 + Note.Model.id; // TODO: Magic number
        CompletedLine.size = new Vector2(1, 0);
        CompletedLine2.size = new Vector2(1, 0);
        SpriteMask = Note.transform.GetComponentInChildren<SpriteMask>();
    }

    protected override void UpdateComponentStates()
    {
        base.UpdateComponentStates();
        if (!Note.IsCleared && Game.Time >= Note.Model.intro_time && Game.Time <= Note.Model.end_time + Note.MissThreshold)
        {
            Line2.enabled = true;
            CompletedLine2.enabled = true;
            CompletedLine2.size = new Vector2(1, 0);
            CompletedLine2.transform.SetLocalScaleX(Line2.transform.localScale.x);
            if (Game.State.Mods.Contains(Mod.HideNotes))
            {
                Line2.enabled = false;
                CompletedLine2.enabled = false;
            }

            if (!Note.IsCleared)
            {
                Line2.flipY = !Line.flipY;
                CompletedLine2.flipY = !CompletedLine.flipY;

                Line.size = new Vector2(1, orthographicSize * 4);
                Line2.size = new Vector2(1, orthographicSize * 4);

                var color = Fill.color;
                CompletedLine.color = color;
                CompletedLine2.color = color;

                var baseSortingOrder = Ring.sortingOrder;
                Line.sortingOrder = baseSortingOrder;
                Line2.sortingOrder = baseSortingOrder;
                CompletedLine.sortingOrder = baseSortingOrder + 1;
                CompletedLine2.sortingOrder = baseSortingOrder + 1;
                SpriteMask.frontSortingOrder = baseSortingOrder + 2;
                SpriteMask.backSortingOrder = baseSortingOrder - 1;
                
                if (HoldNote.Holding)
                {
                    if (Note.Game.Time > Note.Model.start_time)
                    {
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

                        var noteY = Note.transform.position.y;
                        topLine.size = new Vector2(1, (orthographicSize - noteY) * HoldNote.HoldProgress);
                        bottomLine.size = new Vector2(1, -(-orthographicSize - noteY) * HoldNote.HoldProgress);
                    }
                }
            }
        }
        else
        {
            Line2.enabled = false;
            CompletedLine2.enabled = false;
        }
    }

    protected override void UpdateComponentOpacity()
    {
        base.UpdateComponentOpacity();
        Line2.color = Line2.color.WithAlpha(EasedOpacity);
        CompletedLine2.color = CompletedLine2.color.WithAlpha(EasedOpacity);
    }

    protected override void UpdateTransformScale()
    {
        base.UpdateTransformScale();
        Line.size = new Vector2(1, orthographicSize * 4);
        Line2.transform.SetLocalScaleX(Line.transform.localScale.x);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        Object.Destroy(Line2.gameObject);
        Object.Destroy(CompletedLine2.gameObject);
    }
}