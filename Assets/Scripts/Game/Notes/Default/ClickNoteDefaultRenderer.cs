using DragonBones;

public class ClickNoteDefaultRenderer : DefaultNoteRenderer
{
    
    public ClickNoteDefaultRenderer(Note note) : base(note)
    {
    }

    protected override UnityDragonBonesData DragonBonesData() =>
        Note.Model.UseAlternativeColor()
            ? DefaultNoteRendererProvider.Instance.ClickAltDragonBonesData
            : DefaultNoteRendererProvider.Instance.ClickDragonBonesData;

    protected override string IntroAnimationName() => "1a";

    protected override float DragonBonesScaleMultiplier() => 1 / 3.5f;

}