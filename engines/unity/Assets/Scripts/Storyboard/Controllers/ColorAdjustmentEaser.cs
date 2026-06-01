namespace Cytoid.Storyboard.Controllers
{
    public class ColorAdjustmentEaser : StoryboardRendererEaser<ControllerState>
    {
        public ColorAdjustmentEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (!Config.UseEffects) return;
            if (From.ColorAdjustment == null) return;

            var effect = Provider.Effects.ColorAdjustment;
            effect.Enabled = From.ColorAdjustment.Value;
            if (!From.ColorAdjustment.Value) return;

            if (From.Brightness != null)
                effect.Brightness = EaseFloat(From.Brightness, To.Brightness);
            if (From.Saturation != null)
                effect.Saturation = EaseFloat(From.Saturation, To.Saturation);
            if (From.Contrast != null)
                effect.Contrast = EaseFloat(From.Contrast, To.Contrast);
        }
    }
}
