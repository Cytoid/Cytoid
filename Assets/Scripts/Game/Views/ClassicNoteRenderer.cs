using UnityEngine;

public class ClassicNoteRenderer : NoteRenderer
{
    protected float BaseSize;
    protected Color BaseRingColor;
    protected Color BaseFillColor;

    public readonly SpriteRenderer Ring;
    public readonly SpriteRenderer Fill;

    public ClassicNoteRenderer(Note note) : base(note)
    {
        Ring = Note.transform.Find("NoteRing").GetComponent<SpriteRenderer>();
        Fill = Note.transform.Find("NoteFill").GetComponent<SpriteRenderer>();

        Ring.enabled = false;
        Fill.enabled = false;
    }

    public override void OnNoteLoaded()
    {
        var config = Game.Config;

        // Calculate base size
        var sizeMultiplier = config.ChartNoteSizeMultiplier;
        if (Note.Model.size != double.MinValue)
        {
            // Chart note override?
            sizeMultiplier = (float) Note.Model.size * (1 + config.PlayerNoteSizeOffset);
        }

        BaseSize = config.NoteSizes[Note.Type] * sizeMultiplier;

        // Colors
        BaseRingColor = Note.Model.ring_color?.ToColor() ?? config.GetRingColor(Note.Model);
        BaseFillColor = Note.Model.fill_color?.ToColor() ?? config.GetFillColor(Note.Model);

        // Canvas sorting
        Ring.sortingOrder = (Note.Chart.note_list.Count - Note.Model.id) * 3;
        Fill.sortingOrder = Ring.sortingOrder - 1;
    }

    protected override void Render()
    {
        UpdateCollider();
        UpdateComponentStates();
        UpdateColors();
        UpdateTransformScale();
        UpdateFillScale();
        UpdateComponentOpacity();
    }

    protected virtual void UpdateCollider()
    {
        Collider.enabled = Game.Time >= Note.Model.intro_time && Game.Time <= Note.Model.end_time + Note.MissThreshold;
    }

    protected virtual void UpdateComponentStates()
    {
        if (!Note.IsCleared && Game.Time >= Note.Model.intro_time && Game.Time <= Note.Model.end_time + Note.MissThreshold)
        {
            Ring.enabled = true;
            Fill.enabled = true;
            if (Game.State.Mods.Contains(Mod.HideNotes))
            {
                Ring.enabled = false;
                Fill.enabled = false;
            }
        }
        else
        {
            Ring.enabled = false;
            Fill.enabled = false;
        }
    }

    protected virtual void UpdateColors()
    {
        Ring.color = Game.Config.GetRingColorOverride(Note.Model) != Color.clear
            ? Game.Config.GetRingColorOverride(Note.Model)
            : BaseRingColor;
        Fill.color = Game.Config.GetFillColorOverride(Note.Model) != Color.clear
            ? Game.Config.GetFillColorOverride(Note.Model)
            : BaseFillColor;
    }
    
    protected virtual void UpdateTransformScale()
    {
        // Scale entire transform
        const float minPercentageSize = 0.4f;
        var timeRequired = 1.367f / Note.Model.speed;
        var timeScaledSize = BaseSize * minPercentageSize + BaseSize * (1 - minPercentageSize) *
                             Mathf.Clamp((Game.Time - Note.Model.intro_time) / timeRequired, 0f, 1f);

        var transform = Note.transform;
        transform.localScale = new Vector3(timeScaledSize, timeScaledSize, transform.localScale.z);
    }

    protected virtual void UpdateFillScale()
    {
        // Scale fill
        float t;
        if (Note.TimeUntilStart > 0)
            t = Mathf.Clamp((Game.Time - Note.Model.intro_time) / (Note.Model.start_time - Note.Model.intro_time), 0f,
                1f);
        else t = 1f;

        var z = Fill.transform.localScale.z;
        Fill.transform.localScale = Vector3.Lerp(new Vector3(0, 0, z), new Vector3(1, 1, z), t);
    }

    protected float EasedOpacity;

    protected virtual void UpdateComponentOpacity()
    {
        var maxOpacity = (float) Note.Chart.opacity;
        if (Note.Model.opacity != double.MinValue)
        {
            maxOpacity = (float) Note.Model.opacity;
        }

        if (Note.TimeUntilStart > 0)
            EasedOpacity =
                Mathf.Clamp((Game.Time - Note.Model.intro_time) / (Note.Model.start_time - Note.Model.intro_time) * 2f,
                    0f, maxOpacity);
        else EasedOpacity = maxOpacity;

        EasedOpacity *= Game.Config.GlobalOpacityMultiplier;

        Ring.color = Ring.color.WithAlpha(EasedOpacity);
        Fill.color = Fill.color.WithAlpha(EasedOpacity);
    }

    public override void OnClear(NoteGrade grade)
    {
        base.OnClear(grade);
        Game.effectController.PlayClearEffect(this, grade, Note.TimeUntilEnd);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        Object.Destroy(Ring);
        Object.Destroy(Fill);
    }

}