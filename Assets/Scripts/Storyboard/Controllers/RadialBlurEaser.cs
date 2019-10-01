namespace Cytoid.Storyboard.Controllers
{
    public class RadialBlurEaser : StoryboardRendererEaser<ControllerState>
    {
        public RadialBlurEaser()
        {  
            Provider.RadialBlur.Apply(it =>
            {
                it.enabled = false;
                it.Intensity = 0.025f;
            });
        }

        public override void OnUpdate()
        {
            if (Config.UseEffects)
            {
                if (From.RadialBlur.IsSet())
                {
                    Provider.RadialBlur.enabled = From.RadialBlur.Value;
                    if (From.RadialBlur.Value && From.RadialBlurIntensity.IsSet())
                        Provider.RadialBlur.Intensity = EaseFloat(From.RadialBlurIntensity, To.RadialBlurIntensity);
                }
            }
        }
    }
}