namespace Cytoid.Storyboard.Controllers
{
    public class ColorFilterEaser : StoryboardRendererEaser<ControllerState>
    {
        public ColorFilterEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (!Config.UseEffects) return;
            if (From.ColorFilter == null) return;

            var effect = Provider.Effects.ColorFilter;
            effect.Enabled = From.ColorFilter.Value;
            if (From.ColorFilter.Value)
                effect.ColorRgb = EaseColor(From.ColorFilterColor, To.ColorFilterColor);
        }
    }
}
