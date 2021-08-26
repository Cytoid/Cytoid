using System.Linq.Expressions;
using UnityEngine;

public class ClassicLongHoldNoteRenderer : ClassicHoldNoteRenderer
{
    public SpriteRenderer Line2;
    public SpriteRenderer CompletedLine2;
    private float orthographicSize;

    public ClassicLongHoldNoteRenderer(LongHoldNote holdNote) : base(holdNote) => Expression.Empty();

    protected override void InitializeHoldComponents()
    {
        orthographicSize = Game.camera.orthographicSize;
        // Override base renderer
        var provider = ClassicLongHoldNoteRendererProvider.Instance;
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
        Triangle = Object.Instantiate(provider.trianglePrefab, Game.contentParent.transform).GetComponent<MeshTriangle>();
        Triangle.gameObject.SetActive(false);
        HoldFx = Object.Instantiate(Game.effectController.holdFx, Note.transform, false);
        HoldFx.transform.SetLocalScale(HoldFx.transform.localScale.x * (1 + Context.Player.Settings.ClearEffectsSize));
        HoldFx.transform.DeltaZ(-0.001f);
        InitialProgressRingScale = ProgressRing.transform.localScale;
        ProgressRing.maxCutoff = 0;
        ProgressRing.fillCutoff = 0;
        CompletedLine.size = new Vector2(1, 0);
        CompletedLine2.size = new Vector2(1, 0);
        SpriteMask = Note.transform.GetComponentInChildren<SpriteMask>();
    }

    public override void OnNoteLoaded()
    {
        base.OnNoteLoaded();

        ProgressRing.spriteRenderer.material.renderQueue = 3000 + Note.Model.id; // TODO: Magic number
        CompletedLine2.size = new Vector2(1, 0);
        Line.size = new Vector2(1, orthographicSize * 4);
        Line2.size = new Vector2(1, orthographicSize * 4);
    }

    protected override void UpdateComponentStates()
    {
        base.UpdateComponentStates();
        if (!Note.IsCleared && Game.Time >= Note.Model.intro_time + Note.JudgmentOffset && Game.Time <= Note.Model.end_time + Note.MissThreshold + Note.JudgmentOffset)
        {
            Line2.enabled = true;
            CompletedLine2.enabled = true;
            if (Game.State.Mods.Contains(Mod.HideNotes))
            {
                Line2.enabled = false;
                CompletedLine2.enabled = false;
            }

            if (!Note.IsCleared)
            {
                Line2.flipY = !Line.flipY;
                CompletedLine2.flipY = !CompletedLine.flipY;
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
                
                if (HoldNote.IsHolding)
                {
                    if (Note.Game.Time > Note.Model.start_time + Note.JudgmentOffset)
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

                        var noteY = Note.transform.localPosition.y;
                        topLine.size = new Vector2(topLine.size.x, (orthographicSize - noteY) * HoldNote.HoldProgress);
                        bottomLine.size = new Vector2(bottomLine.size.x, -(-orthographicSize - noteY) * HoldNote.HoldProgress);
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
        if (UseExperimentalAnimations)
        {
            if (HoldNote.IsHolding && Note.Game.Time > Note.Model.start_time + Note.JudgmentOffset)
            {
                Line2.color = Line2.color.WithAlpha(0.5f + HoldNote.HoldProgress * 0.5f);
            }
            else
            {
                Line2.color = Line2.color.WithAlpha(EasedOpacity * 0.5f);
            }
        }
        else
        {
            Line2.color = Line2.color.WithAlpha(EasedOpacity);
        }
        CompletedLine2.color = CompletedLine2.color.WithAlpha(EasedOpacity);
    }

    protected override void UpdateTransformScale()
    {
        base.UpdateTransformScale();
        
        Line2.transform.SetLocalScaleX(Line.transform.localScale.x);
        CompletedLine2.transform.SetLocalScaleX(CompletedLine.transform.localScale.x);
    }

    public override void Dispose()
    {
        base.Dispose();
        Object.Destroy(Line2.gameObject);
        Object.Destroy(CompletedLine2.gameObject);
    }
}