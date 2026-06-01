namespace Cytoid.Storyboard.Controllers
{
    public class RadialBlurEaser : StoryboardRendererEaser<ControllerState>
    {
        public RadialBlurEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (!Config.UseEffects) return;

            if (From.RadialBlur == null) return;

            var effect = Provider.Effects.RadialBlur;
            effect.Enabled = From.RadialBlur.Value;
            if (From.RadialBlur.Value && From.RadialBlurIntensity != null)
                effect.Intensity = EaseFloat(From.RadialBlurIntensity, To.RadialBlurIntensity);
        }
    }
}
