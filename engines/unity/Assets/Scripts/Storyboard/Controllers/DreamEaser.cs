namespace Cytoid.Storyboard.Controllers
{
    public class DreamEaser : StoryboardRendererEaser<ControllerState>
    {
        public DreamEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (!Config.UseEffects) return;
            if (From.Dream == null) return;

            var effect = Provider.Effects.Dream;
            effect.Enabled = From.Dream.Value;
            if (From.Dream.Value && From.DreamIntensity != null)
                effect.Distortion = EaseFloat(From.DreamIntensity, To.DreamIntensity);
        }
    }
}
