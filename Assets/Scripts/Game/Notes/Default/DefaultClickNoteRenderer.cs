using DragonBones;

public class DefaultClickNoteRenderer : DefaultNoteRenderer
{
    
    public DefaultClickNoteRenderer(Note note) : base(note)
    {
    }

    protected override UnityDragonBonesData DragonBonesData() =>
        Note.Model.UseAlternativeColor()
            ? DefaultNoteRendererProvider.Instance.ClickAltDragonBonesData
            : DefaultNoteRendererProvider.Instance.ClickDragonBonesData;

    protected override string IntroAnimationName() => "1a";

    protected override float DragonBonesScaleMultiplier() => 1 / 3.5f;

}