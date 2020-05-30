namespace Cytoid.Storyboard.Controllers
{
    public class GlitchEaser : StoryboardRendererEaser<ControllerState>
    {
        public GlitchEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (Config.UseEffects)
            {
                if (From.Glitch != null)
                {
                    Provider.Glitch.enabled = From.Glitch.Value;
                    if (From.Glitch.Value && From.GlitchIntensity != null)
                    {
                        Provider.Glitch.Glitch = EaseFloat(From.GlitchIntensity, To.GlitchIntensity);
                    }
                }
            }
        }
    }
}