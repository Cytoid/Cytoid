namespace Cytoid.Storyboard.Controllers
{
    public class GrayScaleEaser : StoryboardRendererEaser<ControllerState>
    {
        public GrayScaleEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (!Config.UseEffects) return;
            if (From.GrayScale == null) return;

            var effect = Provider.Effects.GrayScale;
            effect.Enabled = From.GrayScale.Value;
            if (From.GrayScale.Value && From.GrayScaleIntensity != null)
                effect.Fade = EaseFloat(From.GrayScaleIntensity, To.GrayScaleIntensity);
        }
    }
}
