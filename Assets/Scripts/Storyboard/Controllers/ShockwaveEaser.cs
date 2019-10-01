namespace Cytoid.Storyboard.Controllers
{
    public class ShockwaveEaser : StoryboardRendererEaser<ControllerState>
    {
        public ShockwaveEaser()
        {
            Provider.Shockwave.Apply(it =>
            {
                it.enabled = false;
                it.TimeX = 1.0f;
                it.Speed = 1;
            });
        }

        public override void OnUpdate()
        {
            if (Config.UseEffects)
            {
                if (From.Shockwave.IsSet())
                {
                    Provider.Shockwave.enabled = From.Shockwave.Value;
                    if (From.Shockwave.Value && From.ShockwaveSpeed.IsSet())
                    {
                        Provider.Shockwave.Speed = EaseFloat(From.ShockwaveSpeed, To.ShockwaveSpeed);
                    }
                    else
                    {
                        Provider.Shockwave.TimeX = 1.0f; // Reset shock wave position
                    }
                }
            }
        }
    }
}