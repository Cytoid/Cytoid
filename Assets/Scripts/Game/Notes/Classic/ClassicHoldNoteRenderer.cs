using DG.Tweening;
using UnityEngine;

public class ClassicHoldNoteRenderer : ClassicNoteRenderer
{
    public HoldNote HoldNote => (HoldNote) Note; 

    public SpriteRenderer Line;
    public SpriteRenderer CompletedLine;
    public ProgressRing ProgressRing;
    public MeshTriangle Triangle;
    public ParticleSystem HoldFx;
    protected SpriteMask SpriteMask;
    protected Vector3 InitialProgressRingScale;

    public ClassicHoldNoteRenderer(HoldNote holdNote) : base(holdNote)
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
        var provider = ClassicHoldNoteRendererProvider.Instance;
        Line = Object.Instantiate(provider.linePrefab, Note.transform, false).GetComponent<SpriteRenderer>();
        Line.color = Line.color.WithAlpha(0);
        CompletedLine = Object.Instantiate(provider.completedLinePrefab, Note.transform, false)
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

        SpriteMask = Note.transform.GetComponentInChildren<SpriteMask>();
    }

    public override void OnNoteLoaded()
    {
        base.OnNoteLoaded();
        Triangle.gameObject.SetActive(true);
        Triangle.Note = Note;
        ProgressRing.spriteRenderer.enabled = true;
        ProgressRing.spriteRenderer.material.renderQueue = 3000 + Note.Model.id; // TODO: Magic number
        var mainModule = HoldFx.main;
        mainModule.startColor = BaseFillColor;
        var newProgressRingScale = InitialProgressRingScale * SizeMultiplier;
        ProgressRing.transform.SetLocalScaleXY(newProgressRingScale.x, newProgressRingScale.y);

        CompletedLine.size = new Vector2(1, 0);
        Line.size = new Vector2(1, 0.21f * Mathf.Floor(Note.Model.holdlength / 0.21f));
    }

    public override void OnCollect()
    {
        base.OnCollect();
        ProgressRing.Reset();
        Triangle.Reset();
        Triangle.gameObject.SetActive(false);
        HoldFx.Stop();
    }

    protected override void UpdateComponentStates()
    {
        base.UpdateComponentStates();
        if (!Note.IsCleared && Game.Time >= Note.Model.intro_time + Note.JudgmentOffset && Game.Time <= Note.Model.end_time + Note.MissThreshold + Note.JudgmentOffset)
        {
            Line.enabled = true;
            CompletedLine.enabled = true;
            CompletedLine.transform.SetLocalScaleX(Line.transform.localScale.x);
            ProgressRing.enabled = true;
            switch (Note.Model.style)
            {
                case 1:
                    Triangle.enabled = true;
                    break;
                case 2:
                    Triangle.enabled = false;
                    break;
            }
            SpriteMask.enabled = true;
            if (HoldNote.IsHolding && Game.Time >= Note.Model.start_time)
            {
                ProgressRing.OnUpdate();
                Triangle.OnUpdate();
            }
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
                    if (Note.Game.Time > Note.Model.start_time + Note.JudgmentOffset)
                    {
                        ProgressRing.fillColor = Fill.color;
                        ProgressRing.maxCutoff = Mathf.Min(1, 1.333f * HoldNote.HoldProgress);
                        ProgressRing.fillCutoff = Mathf.Min(1, HoldNote.HoldProgress);

                        if (UseExperimentalAnimations)
                        {
                            Ring.transform.DOScale(BaseTransformSize * 0.85f, 0.2f);
                            Fill.transform.DOScale(BaseTransformSize * 0.85f, 0.2f);
                            SpriteMask.transform.DOScale(BaseTransformSize * 0.85f, 0.2f);
                        }
                        
                        if (Game.State.IsPlaying && !HoldFx.isPlaying)
                        {
                            var emission = HoldFx.emission;
                            emission.enabled = true;
                            HoldFx.Play();
                        }
                    }
                }
                
                switch (Note.Model.style)
                {
                    case 1:
                    {
                        if (HoldNote.IsHolding)
                        {
                            if (Note.Game.Time > Note.Model.start_time + Note.JudgmentOffset)
                            {
                                CompletedLine.size = new Vector2(1, Note.Model.holdlength * HoldNote.HoldProgress);
                            }
                        }
                        break;
                    }
                    case 2:
                        Line.size = new Vector2(1, Note.Model.holdlength * (1 - HoldNote.HoldProgress));
                        break;
                }
            }
        }
        else
        {
            Line.enabled = false;
            CompletedLine.enabled = false;
            CompletedLine.size = new Vector2(1, 0);
            ProgressRing.enabled = false;
            ProgressRing.Reset();
            Triangle.enabled = false;
            Triangle.Reset();
            SpriteMask.enabled = false;
            if (HoldFx.isPlaying)
            {
                var emission = HoldFx.emission;
                emission.enabled = false;
            }
        }
    }

    protected override void UpdateColors()
    {
        base.UpdateColors();
        var mainModule = HoldFx.main;
        mainModule.startColor = Fill.color;
    }

    protected override void UpdateComponentOpacity()
    {
        base.UpdateComponentOpacity();
        if (UseExperimentalAnimations)
        {
            if (HoldNote.IsHolding && Note.Game.Time > Note.Model.start_time + Note.JudgmentOffset)
            {
                Line.color = Line.color.WithAlpha(0.5f + HoldNote.HoldProgress * 0.5f);
            }
            else
            {
                Line.color = Line.color.WithAlpha(EasedOpacity * 0.5f);
            }
        }
        else
        {
            Line.color = Line.color.WithAlpha(EasedOpacity);
        }
    }

    protected override void UpdateTransformScale()
    {
        if (Game.Time > Note.Model.start_time) return; // Already scaled to maximum
        
        // Scale the entire transform
        var timeScale = Mathf.Clamp((Game.Time - Note.Model.intro_time) / (Note.Model.start_time - Note.Model.intro_time), 0f, 1f);
        
        var minPercentageSize = Note.Model.initial_scale;
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
    }

    public override void Dispose()
    {
        base.Dispose();
        Object.Destroy(Line.gameObject);
        Object.Destroy(CompletedLine.gameObject);
        Object.Destroy(ProgressRing.gameObject);
        Object.Destroy(Triangle.gameObject);
        Object.Destroy(SpriteMask.gameObject);
    }
}