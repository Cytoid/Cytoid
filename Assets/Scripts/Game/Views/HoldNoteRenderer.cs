using UnityEngine;

public class HoldNoteRenderer : ClassicNoteRenderer
{
    public HoldNote HoldNote => (HoldNote) Note; 

    public SpriteRenderer Line;
    public SpriteRenderer CompletedLine;
    public ProgressRingElement ProgressRing;
    public TriangleElement Triangle;

    protected SpriteMask SpriteMask;
    protected int TicksUntilNextHoldEffect;
    protected const int MaxTicksBetweenHoldFx = 9;

    private bool playedEarlyHitSound;

    public HoldNoteRenderer(HoldNote holdNote) : base(holdNote)
    {
        InitializeHoldComponents();
    }

    protected virtual void InitializeHoldComponents()
    {
        var provider = HoldNoteRendererProvider.Instance;
        Line = Object.Instantiate(provider.linePrefab, Note.transform, false).GetComponent<SpriteRenderer>();
        Line.color = Line.color.WithAlpha(0);
        CompletedLine = Object.Instantiate(provider.completedLinePrefab, Note.transform, false)
            .GetComponent<SpriteRenderer>();
        ProgressRing = Object.Instantiate(provider.progressRingPrefab, Note.transform, false)
            .GetComponent<ProgressRingElement>();
        Triangle = Object.Instantiate(provider.trianglePrefab).GetComponent<TriangleElement>();
        ProgressRing.maxCutoff = 0;
        ProgressRing.fillCutoff = 0;
        CompletedLine.size = new Vector2(1, 0);

        SpriteMask = Note.transform.GetComponentInChildren<SpriteMask>();
        TicksUntilNextHoldEffect = MaxTicksBetweenHoldFx;
    }

    public override void OnNoteLoaded()
    {
        base.OnNoteLoaded();
        Triangle.Note = Note.Model;
        // TODO: Magic number
        ProgressRing.gameObject.GetComponent<SpriteRenderer>().material.renderQueue = 3000 + Note.Model.id;
    }

    protected override void UpdateComponentStates()
    {
        base.UpdateComponentStates();
        if (!Note.IsCleared && Game.Time >= Note.Model.intro_time && Game.Time <= Note.Model.end_time + Note.MissThreshold)
        {
            Line.enabled = true;
            CompletedLine.enabled = true;
            CompletedLine.size = new Vector2(1, 0);
            ProgressRing.enabled = true;
            Triangle.enabled = true;
            SpriteMask.enabled = true;
            Triangle.isShowing = HoldNote.Holding && Game.Time >= Note.Model.start_time;
            if (Game.State.Mods.Contains(Mod.HideNotes))
            {
                Line.enabled = false;
                CompletedLine.enabled = false;
                Triangle.enabled = false;
            }
            if (!Note.IsCleared)
            {
                Line.flipY = Note.Model.direction == -1;
                CompletedLine.flipY = Line.flipY;
                CompletedLine.color = Fill.color;
                var ringSortingOrder = Ring.sortingOrder;
                Line.sortingOrder = ringSortingOrder;
                CompletedLine.sortingOrder = ringSortingOrder + 1;
                SpriteMask.frontSortingOrder = CompletedLine.sortingOrder + 1;
                SpriteMask.backSortingOrder = Line.sortingOrder - 1;

                SpriteMask.enabled = Game.Time >= Note.Model.intro_time;

                if (HoldNote.Holding)
                {
                    if (Note.Game.Time > Note.Model.start_time)
                    {
                        if (!playedEarlyHitSound && Context.LocalPlayer.PlayHitSoundsEarly)
                        {
                            playedEarlyHitSound = true;
                            Note.PlayHitSound();
                        }

                        ProgressRing.fillColor = Fill.color;
                        ProgressRing.maxCutoff = Mathf.Min(1, 1.333f * HoldNote.HoldProgress);
                        ProgressRing.fillCutoff = Mathf.Min(1, HoldNote.HoldProgress);
                        CompletedLine.size = new Vector2(1, Note.Model.holdlength * HoldNote.HoldProgress);

                        if (TicksUntilNextHoldEffect == MaxTicksBetweenHoldFx)
                        {
                            TicksUntilNextHoldEffect = 0;
                            Game.effectController.PlayClassicHoldEffect(this);
                        }

                        TicksUntilNextHoldEffect++;
                    }
                }
            }
        }
        else
        {
            Line.enabled = false;
            CompletedLine.enabled = false;
            CompletedLine.size = new Vector2(1, 0);
            ProgressRing.enabled = false;
            Triangle.enabled = false;
            SpriteMask.enabled = false;
        }
    }

    protected override void UpdateComponentOpacity()
    {
        base.UpdateComponentOpacity();
        Line.color = Line.color.WithAlpha(EasedOpacity);
    }

    protected override void UpdateTransformScale()
    {
        // Scale the entire transform
        var timeRequired = 1.367f / Note.Model.speed;
        var timeScale = Mathf.Clamp((Game.Time - Note.Model.intro_time) / timeRequired, 0f, 1f);
        
        const float minPercentageSize = 0.4f;
        var timeScaledSize = BaseSize * minPercentageSize + BaseSize * (1 - minPercentageSize) * timeScale;
        const float minPercentageLineSize = 0.0f;
        var timeScaledLineSize = minPercentageLineSize + (1 - minPercentageLineSize) * timeScale;
        
        Ring.transform.SetLocalScaleXY(timeScaledSize, timeScaledSize);
        Fill.transform.SetLocalScaleXY(timeScaledSize, timeScaledSize);
        SpriteMask.transform.SetLocalScaleXY(timeScaledSize, timeScaledSize);

        Line.transform.SetLocalScaleX(timeScaledLineSize);
        Line.size = new Vector2(1, 0.21f * Mathf.Floor(Note.Model.holdlength / 0.21f));
    }

    protected override void UpdateFillScale()
    {
    }

    public override void Cleanup()
    {
        base.Cleanup();
        Object.Destroy(Line.gameObject);
        Object.Destroy(CompletedLine.gameObject);
        Object.Destroy(ProgressRing.gameObject);
        Object.Destroy(Triangle.gameObject);
        Object.Destroy(SpriteMask.gameObject);
    }
}