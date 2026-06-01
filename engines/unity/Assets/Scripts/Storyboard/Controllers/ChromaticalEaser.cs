namespace Cytoid.Storyboard.Controllers
{
    public class ChromaticalEaser : StoryboardRendererEaser<ControllerState>
    {
        public ChromaticalEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (!Config.UseEffects) return;
            if (From.Chromatical == null) return;

            var effect = Provider.Effects.Chromatical;
            effect.Enabled = From.Chromatical.Value;
            if (From.Chromatical.Value)
            {
                if (From.ChromaticalFade != null)
                    effect.Fade = EaseFloat(From.ChromaticalFade, To.ChromaticalFade);
                if (From.ChromaticalIntensity != null)
                    effect.Intensity = EaseFloat(From.ChromaticalIntensity, To.ChromaticalIntensity);
                if (From.ChromaticalSpeed != null)
                    effect.Speed = EaseFloat(From.ChromaticalSpeed, To.ChromaticalSpeed);
            }
            else
            {
                effect.AnimationTime = 1.0f;
            }
        }
    }
}
