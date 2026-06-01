namespace Cytoid.Storyboard
{
    public class StoryboardConfig
    {
        public Storyboard Storyboard { get; }

        public bool UseEffects = true;

        public StoryboardConfig(Storyboard storyboard)
        {
            Storyboard = storyboard;
        }
    }
}