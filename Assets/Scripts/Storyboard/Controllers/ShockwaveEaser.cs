namespace Cytoid.Storyboard.Controllers
{
    public class ShockwaveEaser : StoryboardRendererEaser<ControllerState>
    {
        public ShockwaveEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (Config.UseEffects)
            {
                if (From.Shockwave != null)
                {
                    Provider.Shockwave.enabled = From.Shockwave.Value;
                    if (From.Shockwave.Value && From.ShockwaveSpeed != null)
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