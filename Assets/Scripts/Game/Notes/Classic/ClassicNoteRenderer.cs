using UnityEngine;
using Object = UnityEngine.Object;

public class ClassicNoteRenderer : NoteRenderer
{
    protected float SizeMultiplier;
    protected float BaseTransformSize;
    protected Color BaseRingColor;
    protected Color BaseFillColor;

    public readonly SpriteRenderer Ring;
    public readonly SpriteRenderer Fill;

    protected readonly bool DisplayNoteId;
    protected readonly NoteId NoteId;

    public ClassicNoteRenderer(Note note) : base(note)
    {
        Ring = Note.transform.Find("NoteRing").GetComponent<SpriteRenderer>();
        Fill = Note.transform.Find("NoteFill").GetComponent<SpriteRenderer>();

        Ring.enabled = false;
        Fill.enabled = false;
        
        // Generate note ID
        if (Game.Config.DisplayNoteIds || Game is PlayerGame)
        {
            DisplayNoteId = true;
            NoteId = Object.Instantiate(GameObjectProvider.Instance.noteIdPrefab, Note.transform);
            NoteId.SetModel(note.Model);
            NoteId.Visible = Game.Config.DisplayNoteIds || (Game is PlayerGame playerGame && !playerGame.HideInterface);
            NoteId.gameObject.SetActive(false);
        }
    }

    public override void OnNoteLoaded()
    {
        var config = Game.Config;

        // Calculate base size
        SizeMultiplier = Game.Config.GlobalNoteSizeMultiplier;
        if (Note.Model.size != double.MinValue)
        {
            // Chart note override?
            SizeMultiplier = (float) Note.Model.size / (float) Game.Chart.Model.size * SizeMultiplier;
        }

        BaseTransformSize = config.NoteSizes[Note.Type] * SizeMultiplier;

        // Colors
        BaseRingColor = Note.Model.ring_color?.ToColor() ?? config.GetRingColor(Note.Model);
        BaseFillColor = Note.Model.fill_color?.ToColor() ?? config.GetFillColor(Note.Model);

        // Canvas sorting
        Ring.sortingOrder = (Note.Chart.note_list.Count - Note.Model.id) * 3;
        Fill.sortingOrder = Ring.sortingOrder - 1;
    }

    protected override void Render()
    {
        if (NoteId != null) NoteId.Visible = !(Game is PlayerGame playerGame) || !playerGame.HideInterface;
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
            if (Game.State.Mods.Contains(Mod.HideNotes))
            {
                Ring.enabled = false;
                Fill.enabled = false;
            }
            else
            {
                Ring.enabled = true;
                Fill.enabled = true;
                if (DisplayNoteId)
                {
                    NoteId.gameObject.SetActive(true);
                    NoteId.transform.localEulerAngles = new Vector3(0, 0, -Note.transform.localEulerAngles.z);
                }
            }
        }
        else
        {
            Ring.enabled = false;
            Fill.enabled = false;
            if (DisplayNoteId)
            {
                NoteId.gameObject.SetActive(false);
            }
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
        var sizeMultiplier = Note.Model.Override.SizeMultiplier;

        var transformSize = BaseTransformSize * sizeMultiplier;
        
        // Scale entire transform
        var minPercentageSize = Note.Model.initial_scale;
        var timeScaledSize = transformSize * minPercentageSize + transformSize * (1 - minPercentageSize) *
                             Mathf.Clamp((Game.Time - Note.Model.intro_time) / (Note.Model.start_time - Note.Model.intro_time), 0f, 1f);

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
        
        EasedOpacity *= Game.Config.GlobalNoteOpacityMultiplier;
        EasedOpacity *= Note.Model.Override.OpacityMultiplier;

        Ring.color = Ring.color.WithAlpha(EasedOpacity);
        Fill.color = Fill.color.WithAlpha(EasedOpacity);
    }

    public override void OnClear(NoteGrade grade)
    {
        base.OnClear(grade);
        Game.effectController.PlayClearEffect(this, grade, Note.TimeUntilEnd + Note.JudgmentOffset);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        Object.Destroy(Ring);
        Object.Destroy(Fill);
    }

}