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
                if (From.Fisheye.IsSet())
                {
                    Provider.Fisheye.enabled = From.Fisheye.Value;
                    if (From.Fisheye.Value && From.FisheyeIntensity.IsSet())
                    {
                        Provider.Fisheye.Distortion = EaseFloat(From.FisheyeIntensity, To.FisheyeIntensity);
                    }
                }
            }
        }
    }
}