namespace Cytoid.Storyboard.Controllers
{
    public class FocusEaser : StoryboardRendererEaser<ControllerState>
    {
        public FocusEaser()
        {  
            Provider.Focus.Apply(it =>
            {
                it.enabled = false;
                it.Size = 1;
                it.Color = UnityEngine.Color.white;
                it.Speed = 5;
                it.Intensity = 0.25f;
            });
        }

        public override void OnUpdate()
        {
            if (Config.UseEffects)
            {
                if (From.Focus.IsSet())
                {
                    Provider.Focus.enabled = From.Focus.Value;
                    if (From.Focus.Value)
                    {
                        if (From.FocusIntensity.IsSet())
                        {
                            Provider.Focus.Intensity = EaseFloat(From.FocusIntensity, To.FocusIntensity);
                        }
                        if (From.FocusSize.IsSet())
                        {
                            Provider.Focus.Size = EaseFloat(From.FocusSize, To.FocusSize);
                        }
                        if (From.FocusSpeed.IsSet())
                        {
                            Provider.Focus.Speed = EaseFloat(From.FocusSpeed, To.FocusSpeed);
                        }
                        if (From.FocusColor.IsSet())
                        {
                            Provider.Focus.Color = EaseColor(From.FocusColor, To.FocusColor);
                        }
                    }
                }
            }
        }
    }
}