namespace Cytoid.Storyboard.PostProcess
{
    public interface IStoryboardEffects
    {
        StoryboardEffectsChannels Channels { get; }

        void ResetToDefaults();
    }
}
