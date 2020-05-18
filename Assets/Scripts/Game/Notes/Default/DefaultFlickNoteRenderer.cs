using DragonBones;

public class DefaultFlickNoteRenderer : DefaultNoteRenderer
{
    
    public DefaultFlickNoteRenderer(Note note) : base(note)
    {
    }
    
    protected override UnityDragonBonesData DragonBonesData() =>
        DefaultNoteRendererProvider.Instance.FlickDragonBonesData;

    protected override string IntroAnimationName() => "4a";

    protected override float DragonBonesScaleMultiplier() => 1 / 5f;

}