namespace Cytoid.Storyboard.Controllers
{
    public class FisheyeEaser : StoryboardRendererEaser<ControllerState>
    {
        public FisheyeEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (!Config.UseEffects) return;
            if (From.Fisheye == null) return;

            var effect = Provider.Effects.Fisheye;
            effect.Enabled = From.Fisheye.Value;
            if (From.Fisheye.Value && From.FisheyeIntensity != null)
                effect.Distortion = EaseFloat(From.FisheyeIntensity, To.FisheyeIntensity);
        }
    }
}
