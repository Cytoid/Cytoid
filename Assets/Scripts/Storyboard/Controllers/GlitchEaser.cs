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
                if (From.Glitch.IsSet())
                {
                    Provider.Glitch.enabled = From.Glitch.Value;
                    if (From.Glitch.Value && From.GlitchIntensity.IsSet())
                    {
                        Provider.Glitch.Glitch = EaseFloat(From.GlitchIntensity, To.GlitchIntensity);
                    }
                }
            }
        }
    }
}