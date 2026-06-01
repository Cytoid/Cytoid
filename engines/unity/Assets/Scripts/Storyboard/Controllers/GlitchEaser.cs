namespace Cytoid.Storyboard.Controllers
{
    public class GlitchEaser : StoryboardRendererEaser<ControllerState>
    {
        public GlitchEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (!Config.UseEffects) return;
            if (From.Glitch == null) return;

            var effect = Provider.Effects.Glitch;
            effect.Enabled = From.Glitch.Value;
            if (From.Glitch.Value && From.GlitchIntensity != null)
                effect.Glitch = EaseFloat(From.GlitchIntensity, To.GlitchIntensity);
        }
    }
}
