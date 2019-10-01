namespace Cytoid.Storyboard.Controllers
{
    public class NoiseEaser : StoryboardRendererEaser<ControllerState>
    {
        public NoiseEaser()
        {  
            Provider.Noise.Apply(it =>
            {
                it.enabled = false;
                it.Noise = 0.2f;
            });
        }

        public override void OnUpdate()
        {
            if (Config.UseEffects)
            {
                if (From.Noise.IsSet())
                {
                    Provider.Noise.enabled = From.Noise.Value;
                    if (From.Noise.Value && From.NoiseIntensity.IsSet())
                    {
                        Provider.Noise.Noise = EaseFloat(From.NoiseIntensity, To.NoiseIntensity);
                    }
                }
            }
        }
    }
}