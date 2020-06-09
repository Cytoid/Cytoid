using DragonBones;
using UnityEngine;

public class DefaultHoldNoteRenderer : DefaultNoteRenderer
{
    
    public HoldNote HoldNote => (HoldNote) Note; 
    
    public SpriteRenderer Line;
    public SpriteRenderer CompletedLine;
    public ProgressRing ProgressRing;
    public MeshTriangle Triangle;

    public Color ProgressFillColor;

    protected float HeldTimestamp;
    
    public DefaultHoldNoteRenderer(Note note) : base(note)
    {
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
        var newProgressRingScale = ProgressRing.transform.localScale.x * SizeMultiplier;
        ProgressRing.transform.SetLocalScaleXY(newProgressRingScale, newProgressRingScale);
        ProgressRing.maxCutoff = 0;
        ProgressRing.fillCutoff = 0;
        CompletedLine.size = new Vector2(1, 0);
    }

    public override void OnNoteLoaded()
    {
        base.OnNoteLoaded();
        InitializeHoldComponents();
        Triangle.Note = Note;
        // TODO: Magic number
        ProgressRing.gameObject.GetComponent<SpriteRenderer>().material.renderQueue = 3000 + Note.Model.id;
        CompletedLine.size = new Vector2(1, 0);
        Line.size = new Vector2(1, 0.21f * Mathf.Floor(Note.Model.holdlength / 0.21f));
        ProgressFillColor = "#efc65a".ToColor();
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
                CompletedLine.color = ProgressFillColor;
                var ringSortingOrder = (Note.Chart.note_list.Count - Note.Model.id) * 3;
                Line.sortingOrder = ringSortingOrder;
                CompletedLine.sortingOrder = ringSortingOrder + 1;

                if (HoldNote.IsHolding)
                {
                    if (Note.Game.Time > Note.Model.start_time)
                    {
                        ProgressRing.fillColor = ProgressFillColor;
                        ProgressRing.maxCutoff = Mathf.Min(1, 1.333f * HoldNote.HoldProgress);
                        ProgressRing.fillCutoff = Mathf.Min(1, HoldNote.HoldProgress);
                        CompletedLine.size = new Vector2(1, Note.Model.holdlength * HoldNote.HoldProgress);

                        if (HeldTimestamp == default)
                        {
                            HeldTimestamp = Game.Time;
                        }
                        
                        // t
                        var t = (Game.Time - HeldTimestamp) % 1.666667f;

                        if (t > 0)
                        {
                            var time = t * 1.666667f;
                            ArmatureComponent.animation.GotoAndStopByTime("3b", time);
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
        }
        
        // Scale the entire transform
        var timeScale = Mathf.Clamp((Game.Time - Note.Model.intro_time) / (Note.Model.start_time - Note.Model.intro_time), 0f, 1f);
        
        const float minPercentageLineSize = 0.0f;
        var timeScaledLineSize = minPercentageLineSize + (1 - minPercentageLineSize) * timeScale;

        Line.transform.SetLocalScaleX(timeScaledLineSize * SizeMultiplier);
    }

    protected override void UpdateComponentOpacity()
    {
        base.UpdateComponentOpacity();
        Line.color = Line.color.WithAlpha(EasedOpacity);
    }

    public override void Dispose()
    {
        base.Dispose();
        Object.Destroy(Line.gameObject);
        Object.Destroy(CompletedLine.gameObject);
        Object.Destroy(ProgressRing.gameObject);
        Object.Destroy(Triangle.gameObject);
    }
    
    protected override UnityDragonBonesData DragonBonesData() =>
        DefaultNoteRendererProvider.Instance.HoldDragonBonesData;

    protected override string IntroAnimationName() => "3a";

    protected override float DragonBonesScaleMultiplier() => 1 / 4f;
}