namespace Cytoid.Storyboard.Controllers
{
    public class BloomEaser : StoryboardRendererEaser<ControllerState>
    {
        public BloomEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (!Config.UseEffects) return;
            if (From.Bloom == null) return;

            var bloom = Provider.Effects.Bloom;
            bloom.Enabled = From.Bloom.Value;
            if (From.Bloom.Value && From.BloomIntensity != null)
                bloom.Intensity = EaseFloat(From.BloomIntensity, To.BloomIntensity);
        }
    }
}
