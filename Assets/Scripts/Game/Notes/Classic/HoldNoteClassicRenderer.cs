using UnityEngine;

public class HoldNoteClassicRenderer : ClassicNoteRenderer
{
    public HoldNote HoldNote => (HoldNote) Note; 

    public SpriteRenderer Line;
    public SpriteRenderer CompletedLine;
    public ProgressRing ProgressRing;
    public MeshTriangle Triangle;

    protected SpriteMask SpriteMask;
    protected float NextHoldEffectTimestamp;

    private bool playedEarlyHitSound;

    public HoldNoteClassicRenderer(HoldNote holdNote) : base(holdNote)
    {
        InitializeHoldComponents();
    }

    public override void OnLateUpdate()
    {
        base.OnLateUpdate();
        if (HoldNote.IsHolding) Collider.radius *= 1.3333f;
    }

    protected virtual void InitializeHoldComponents()
    {
        var provider = HoldNoteClassicRendererProvider.Instance;
        Line = Object.Instantiate(provider.linePrefab, Note.transform, false).GetComponent<SpriteRenderer>();
        Line.color = Line.color.WithAlpha(0);
        CompletedLine = Object.Instantiate(provider.completedLinePrefab, Note.transform, false)
            .GetComponent<SpriteRenderer>();
        ProgressRing = Object.Instantiate(provider.progressRingPrefab, Note.transform, false)
            .GetComponent<ProgressRing>();
        Triangle = Object.Instantiate(provider.trianglePrefab).GetComponent<MeshTriangle>();
        var newProgressRingScale = ProgressRing.transform.localScale.x * SizeMultiplier;
        ProgressRing.transform.SetLocalScaleXY(newProgressRingScale, newProgressRingScale);
        ProgressRing.maxCutoff = 0;
        ProgressRing.fillCutoff = 0;
        CompletedLine.size = new Vector2(1, 0);

        SpriteMask = Note.transform.GetComponentInChildren<SpriteMask>();
    }

    public override void OnNoteLoaded()
    {
        base.OnNoteLoaded();
        Triangle.Note = Note.Model;
        // TODO: Magic number
        ProgressRing.gameObject.GetComponent<SpriteRenderer>().material.renderQueue = 3000 + Note.Model.id;
        CompletedLine.size = new Vector2(1, 0);
        Line.size = new Vector2(1, 0.21f * Mathf.Floor(Note.Model.holdlength / 0.21f));
    }

    protected override void UpdateComponentStates()
    {
        base.UpdateComponentStates();
        if (!Note.IsCleared && Game.Time >= Note.Model.intro_time && Game.Time <= Note.Model.end_time + Note.MissThreshold)
        {
            Line.enabled = true;
            CompletedLine.enabled = true;
            CompletedLine.transform.SetLocalScaleX(Line.transform.localScale.x);
            ProgressRing.enabled = true;
            Triangle.enabled = true;
            SpriteMask.enabled = true;
            Triangle.isShowing = HoldNote.IsHolding && Game.Time >= Note.Model.start_time;
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

                if (HoldNote.IsHolding)
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

                        if (Time.realtimeSinceStartup >= NextHoldEffectTimestamp && Game.State.IsPlaying)
                        {
                            NextHoldEffectTimestamp = Time.realtimeSinceStartup + 1f / 8f;
                            Game.effectController.PlayClassicHoldEffect(this);
                        }
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
        var timeScaledSize = BaseTransformSize * minPercentageSize + BaseTransformSize * (1 - minPercentageSize) * timeScale;
        const float minPercentageLineSize = 0.0f;
        var timeScaledLineSize = minPercentageLineSize + (1 - minPercentageLineSize) * timeScale;
        
        Ring.transform.SetLocalScaleXY(timeScaledSize, timeScaledSize);
        Fill.transform.SetLocalScaleXY(timeScaledSize, timeScaledSize);
        SpriteMask.transform.SetLocalScaleXY(timeScaledSize, timeScaledSize);

        Line.transform.SetLocalScaleX(timeScaledLineSize * SizeMultiplier);
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