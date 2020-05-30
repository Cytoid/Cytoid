namespace Cytoid.Storyboard.Controllers
{
    public class FocusEaser : StoryboardRendererEaser<ControllerState>
    {
        public FocusEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (Config.UseEffects)
            {
                if (From.Focus != null)
                {
                    Provider.Focus.enabled = From.Focus.Value;
                    if (From.Focus.Value)
                    {
                        if (From.FocusIntensity != null)
                        {
                            Provider.Focus.Intensity = EaseFloat(From.FocusIntensity, To.FocusIntensity);
                        }
                        if (From.FocusSize != null)
                        {
                            Provider.Focus.Size = EaseFloat(From.FocusSize, To.FocusSize);
                        }
                        if (From.FocusSpeed != null)
                        {
                            Provider.Focus.Speed = EaseFloat(From.FocusSpeed, To.FocusSpeed);
                        }
                        if (From.FocusColor != null)
                        {
                            Provider.Focus.Color = EaseColor(From.FocusColor, To.FocusColor);
                        }
                    }
                }
            }
        }
    }
}