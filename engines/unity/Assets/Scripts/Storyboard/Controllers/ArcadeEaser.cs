namespace Cytoid.Storyboard.Controllers
{
    public class ArcadeEaser : StoryboardRendererEaser<ControllerState>
    {
        public ArcadeEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (!Config.UseEffects) return;
            if (From.Arcade == null) return;

            var effect = Provider.Effects.Arcade;
            effect.Enabled = From.Arcade.Value;
            if (!From.Arcade.Value) return;

            if (From.ArcadeIntensity != null)
                effect.Fade = EaseFloat(From.ArcadeIntensity, To.ArcadeIntensity);
            if (From.ArcadeInterferanceSize != null)
                effect.InterferanceSize = EaseFloat(From.ArcadeInterferanceSize, To.ArcadeInterferanceSize);
            if (From.ArcadeInterferanceSpeed != null)
                effect.InterferanceSpeed = EaseFloat(From.ArcadeInterferanceSpeed, To.ArcadeInterferanceSpeed);
            if (From.ArcadeContrast != null)
                effect.Contrast = EaseFloat(From.ArcadeContrast, To.ArcadeContrast);
        }
    }
}
