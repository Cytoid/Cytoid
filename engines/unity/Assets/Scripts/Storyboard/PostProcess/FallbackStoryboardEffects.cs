namespace Cytoid.Storyboard.PostProcess
{
    public sealed class FallbackStoryboardEffects : IStoryboardEffects
    {
        public StoryboardEffectsChannels Channels { get; }

        public FallbackStoryboardEffects(StoryboardFallbackPostProcess postProcess)
        {
            Channels = new StoryboardEffectsChannels();
            postProcess.Bind(Channels);
        }

        public void ResetToDefaults()
        {
            Channels.ResetToDefaults();
        }
    }
}
