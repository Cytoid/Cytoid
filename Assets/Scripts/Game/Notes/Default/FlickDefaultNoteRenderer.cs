using DragonBones;

public class FlickDefaultNoteRenderer : DefaultNoteRenderer
{
    
    public FlickDefaultNoteRenderer(Note note) : base(note)
    {
    }
    
    protected override UnityDragonBonesData DragonBonesData() =>
        DefaultNoteRendererProvider.Instance.FlickDragonBonesData;

    protected override string IntroAnimationName() => "4a";

    protected override float DragonBonesScaleMultiplier() => 1 / 5f;

}