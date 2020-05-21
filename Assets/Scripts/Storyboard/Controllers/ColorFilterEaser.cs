namespace Cytoid.Storyboard.Controllers
{
    public class ColorFilterEaser : StoryboardRendererEaser<ControllerState>
    {
        public ColorFilterEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (Config.UseEffects)
            {
                if (From.ColorFilter.IsSet())
                {
                    Provider.ColorFilter.enabled = From.ColorFilter.Value;
                    if (From.ColorFilter.Value && From.ColorFilterColor.IsSet())
                    {
                        Provider.ColorFilter.ColorRGB = EaseColor(From.ColorFilterColor, To.ColorFilterColor);
                    }
                }
            }
        }
    }
}