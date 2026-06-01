namespace Cytoid.Storyboard.Controllers
{
    public class FocusEaser : StoryboardRendererEaser<ControllerState>
    {
        public FocusEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (!Config.UseEffects) return;
            if (From.Focus == null) return;

            var effect = Provider.Effects.Focus;
            effect.Enabled = From.Focus.Value;
            if (!From.Focus.Value) return;

            if (From.FocusIntensity != null)
                effect.Intensity = EaseFloat(From.FocusIntensity, To.FocusIntensity);
            if (From.FocusSize != null)
                effect.Size = EaseFloat(From.FocusSize, To.FocusSize);
            if (From.FocusSpeed != null)
                effect.Speed = EaseFloat(From.FocusSpeed, To.FocusSpeed);
            if (From.FocusColor != null)
                effect.Color = EaseColor(From.FocusColor, To.FocusColor);
        }
    }
}
