namespace Cytoid.Storyboard.Controllers
{
    public class DreamEaser : StoryboardRendererEaser<ControllerState>
    {
        public DreamEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (Config.UseEffects)
            {
                if (From.Dream.IsSet())
                {
                    Provider.Dream.enabled = From.Dream.Value;
                    if (From.Dream.Value && From.DreamIntensity.IsSet())
                    {
                        Provider.Dream.Distortion = EaseFloat(From.DreamIntensity, To.DreamIntensity);
                    }
                }
            }
        }
    }
}