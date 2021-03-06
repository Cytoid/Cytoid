namespace Cytoid.Storyboard.Controllers
{
    public class NoiseEaser : StoryboardRendererEaser<ControllerState>
    {
        public NoiseEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (Config.UseEffects)
            {
                if (From.Noise != null)
                {
                    Provider.Noise.enabled = From.Noise.Value;
                    if (From.Noise.Value && From.NoiseIntensity != null)
                    {
                        Provider.Noise.Noise = EaseFloat(From.NoiseIntensity, To.NoiseIntensity);
                    }
                }
            }
        }
    }
}