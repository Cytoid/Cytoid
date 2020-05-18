using DragonBones;

public class DefaultDragChildNoteRenderer : DefaultNoteRenderer
{
    
    public DefaultDragChildNoteRenderer(Note note) : base(note)
    {
    }
    
    protected override UnityDragonBonesData DragonBonesData() =>
        DefaultNoteRendererProvider.Instance.DragDragonBonesData;

    protected override string IntroAnimationName() => "小点出现";

    protected override float DragonBonesScaleMultiplier() => 1 / 3.5f;

}