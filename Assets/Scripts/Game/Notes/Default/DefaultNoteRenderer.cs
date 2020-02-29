using System.Runtime.InteropServices;
using DragonBones;
using UnityEngine;
using Transform = UnityEngine.Transform;

public abstract class DefaultNoteRenderer : NoteRenderer
{
    
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
        foreach (Transform child in Note.gameObject.transform) Object.Destroy(child.gameObject);

        var config = Game.Config;

        // Calculate base size
        SizeMultiplier = Game.Config.NoteSizeMultiplier;
        if (Note.Model.size != double.MinValue)
        {
            // Chart note override?
            SizeMultiplier = (float) Note.Model.size / (float) Game.Chart.Model.size * SizeMultiplier;
        }

        BaseTransformSize = config.NoteSizes[Note.Type] * SizeMultiplier;

        // Colors
        BaseRingColor = Note.Model.ring_color?.ToColor() ?? config.GetRingColor(Note.Model);
        BaseFillColor = Note.Model.fill_color?.ToColor() ?? config.GetFillColor(Note.Model);

        var dbGameObject = new GameObject();
        dbGameObject.transform.parent = Note.gameObject.transform;
        dbGameObject.transform.SetLocalScale(BaseTransformSize * DragonBonesScaleMultiplier());
        ArmatureComponent = UnityFactory.factory.BuildArmatureComponent(
            "Armature", 
            DragonBonesData().dataName,
            gameObject: dbGameObject
        );
        ArmatureComponent.animation.Play();
        ArmatureComponent.animation.Stop();
        ArmatureComponent.color.alphaMultiplier = 0f;
        ArmatureComponent.sortingOrder =  (Note.Chart.note_list.Count - Note.Model.id) * 3;
        
        Note.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Content"));
    }

    protected override void Render()
    {
        UpdateCollider();
        UpdateComponentStates();
        UpdateComponentOpacity();
    }

    protected virtual void UpdateCollider()
    {
        Collider.enabled = Game.Time >= Note.Model.intro_time && Game.Time <= Note.Model.end_time + Note.MissThreshold;
    }

    protected virtual void UpdateComponentStates()
    {
        if (!Game.State.Mods.Contains(Mod.HideNotes)
            && !Note.IsCleared && Game.Time >= Note.Model.intro_time && Game.Time < Note.Model.start_time)
        {
            // t
            var t = Mathf.Clamp((Game.Time - Note.Model.intro_time) / (Note.Model.start_time - Note.Model.intro_time),
                0f,
                2f);

            if (t > 0)
            {
                ArmatureComponent.color.alphaMultiplier = 1f;
                var time = t * 1.38f;
                ArmatureComponent.animation.GotoAndStopByTime(IntroAnimationName(), time);
            }
        }
        else
        {
            ArmatureComponent.color.alphaMultiplier = 0f;
        }
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
    }

    public override void OnClear(NoteGrade grade)
    {
        base.OnClear(grade);
        Game.effectController.PlayClearEffect(this, grade, Note.TimeUntilEnd);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        //cleanup db
    }

    protected abstract UnityDragonBonesData DragonBonesData();

    protected abstract string IntroAnimationName();

    protected abstract float DragonBonesScaleMultiplier();

}