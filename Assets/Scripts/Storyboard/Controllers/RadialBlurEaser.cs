namespace Cytoid.Storyboard.Controllers
{
    public class RadialBlurEaser : StoryboardRendererEaser<ControllerState>
    {
        public RadialBlurEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (Config.UseEffects)
            {
                if (From.RadialBlur != null)
                {
                    Provider.RadialBlur.enabled = From.RadialBlur.Value;
                    if (From.RadialBlur.Value && From.RadialBlurIntensity != null)
                        Provider.RadialBlur.Intensity = EaseFloat(From.RadialBlurIntensity, To.RadialBlurIntensity);
                }
            }
        }
    }
}