using System.Runtime.InteropServices;
using DragonBones;
using UnityEngine;

public class DefaultNoteRenderer : NoteRenderer
{
    private const float MagicNumber = 3.5f;
    protected float SizeMultiplier;
    protected float BaseTransformSize;
    protected Color BaseRingColor;
    protected Color BaseFillColor;

    protected UnityArmatureComponent ArmatureComponent;

    public DefaultNoteRenderer(Note note) : base(note)
    {
    }

    public override void OnNoteLoaded()
    {
        foreach (UnityEngine.Transform child in Note.gameObject.transform) Object.Destroy(child.gameObject);

        var config = Game.Config;

        // Calculate base size
        SizeMultiplier = Game.Config.NoteSizeMultiplier;
        if (Note.Model.size != double.MinValue)
        {
            // Chart note override?
            SizeMultiplier = (float) Note.Model.size / (float) Game.Chart.Model.size * SizeMultiplier;
        }
        
        BaseTransformSize = config.NoteSizes[Note.Type] * SizeMultiplier / MagicNumber;

        // Colors
        BaseRingColor = Note.Model.ring_color?.ToColor() ?? config.GetRingColor(Note.Model);
        BaseFillColor = Note.Model.fill_color?.ToColor() ?? config.GetFillColor(Note.Model);

        ArmatureComponent = UnityFactory.factory.BuildArmatureComponent("Armature", gameObject: Note.gameObject);
        ArmatureComponent.animation.Play();
        ArmatureComponent.animation.Stop();
        ArmatureComponent.color.alphaMultiplier = 0f;
        
        Note.transform.localScale = new Vector3(BaseTransformSize, BaseTransformSize, 6);
        Note.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Content"));

        ArmatureComponent.sortingOrder = Note.Model.id - 5000; // -5000 to 5000 reserved for notes
    }

    protected override void Render()
    {
        UpdateCollider();
        UpdateComponentStates();
    }

    protected virtual void UpdateCollider()
    {
        Collider.radius *= MagicNumber;
        Collider.enabled = Game.Time >= Note.Model.intro_time && Game.Time <= Note.Model.end_time + Note.MissThreshold;
    }

    protected virtual void UpdateComponentStates()
    {
        if (!Game.State.Mods.Contains(Mod.HideNotes)
            && !Note.IsCleared && Game.Time >= Note.Model.intro_time &&
            Game.Time <= Note.Model.end_time + Note.MissThreshold)
        {
            // t
            var t = Mathf.Clamp((Game.Time - Note.Model.intro_time) / (Note.Model.start_time - Note.Model.intro_time),
                0f,
                2f);

            if (t > 0)
            {
                ArmatureComponent.color.alphaMultiplier = 1f;
                var time = t * 1.38f;
                ArmatureComponent.animation.GotoAndStopByTime("1a", time);
            }
        }
        else
        {
            ArmatureComponent.color.alphaMultiplier = 0f;
        }
    }

    public override void OnClear(NoteGrade grade)
    {
        base.OnClear(grade);
        Game.effectController.PlayClearEffect(this, grade, Note.TimeUntilEnd);
    }

    public override void Cleanup()
    {
        base.Cleanup();
    }
}