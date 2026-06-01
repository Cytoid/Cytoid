namespace Cytoid.Storyboard.Controllers
{
    public class SepiaEaser : StoryboardRendererEaser<ControllerState>
    {
        public SepiaEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (!Config.UseEffects) return;
            if (From.Sepia == null) return;

            var effect = Provider.Effects.Sepia;
            effect.Enabled = From.Sepia.Value;
            if (From.Sepia.Value && From.SepiaIntensity != null)
                effect.Fade = EaseFloat(From.SepiaIntensity, To.SepiaIntensity);
        }
    }
}
