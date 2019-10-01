namespace Cytoid.Storyboard.Controllers
{
    public class GrayScaleEaser : StoryboardRendererEaser<ControllerState>
    {
        public GrayScaleEaser()
        {  
            Provider.GrayScale.Apply(it =>
            {
                it.enabled = false;
                it._Fade = 1;
            });
        }

        public override void OnUpdate()
        {
            if (Config.UseEffects)
            {
                if (From.GrayScale.IsSet())
                {
                    Provider.GrayScale.enabled = From.GrayScale.Value;
                    if (From.GrayScale.Value && From.GrayScaleIntensity.IsSet())
                    {
                        Provider.GrayScale._Fade = EaseFloat(From.GrayScaleIntensity, To.GrayScaleIntensity);
                    }
                }
            }
        }
    }
}