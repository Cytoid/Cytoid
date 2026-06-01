namespace Cytoid.Storyboard.Controllers
{
    public class NoiseEaser : StoryboardRendererEaser<ControllerState>
    {
        public NoiseEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (!Config.UseEffects) return;
            if (From.Noise == null) return;

            var effect = Provider.Effects.Noise;
            effect.Enabled = From.Noise.Value;
            if (From.Noise.Value && From.NoiseIntensity != null)
                effect.Noise = EaseFloat(From.NoiseIntensity, To.NoiseIntensity);
        }
    }
}
