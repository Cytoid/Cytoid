namespace Cytoid.Storyboard.Controllers
{
    public class ShockwaveEaser : StoryboardRendererEaser<ControllerState>
    {
        public ShockwaveEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (!Config.UseEffects) return;
            if (From.Shockwave == null) return;

            var effect = Provider.Effects.Shockwave;
            effect.Enabled = From.Shockwave.Value;
            if (From.Shockwave.Value && From.ShockwaveSpeed != null)
                effect.Speed = EaseFloat(From.ShockwaveSpeed, To.ShockwaveSpeed);
            else
                effect.TimeX = 1f;
        }
    }
}
