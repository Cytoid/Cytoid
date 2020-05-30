namespace Cytoid.Storyboard.Controllers
{
    public class FisheyeEaser : StoryboardRendererEaser<ControllerState>
    {
        public FisheyeEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (Config.UseEffects)
            {
                if (From.Fisheye != null)
                {
                    Provider.Fisheye.enabled = From.Fisheye.Value;
                    if (From.Fisheye.Value && From.FisheyeIntensity != null)
                    {
                        Provider.Fisheye.Distortion = EaseFloat(From.FisheyeIntensity, To.FisheyeIntensity);
                    }
                }
            }
        }
    }
}