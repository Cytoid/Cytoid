namespace Cytoid.Storyboard.Controllers
{
    public class GrayScaleEaser : StoryboardRendererEaser<ControllerState>
    {
        public GrayScaleEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (Config.UseEffects)
            {
                if (From.GrayScale != null)
                {
                    Provider.GrayScale.enabled = From.GrayScale.Value;
                    if (From.GrayScale.Value && From.GrayScaleIntensity != null)
                    {
                        Provider.GrayScale._Fade = EaseFloat(From.GrayScaleIntensity, To.GrayScaleIntensity);
                    }
                }
            }
        }
    }
}