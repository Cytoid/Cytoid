namespace Cytoid.Storyboard.Controllers
{
    public class TapeEaser : StoryboardRendererEaser<ControllerState>
    {
        public TapeEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (!Config.UseEffects) return;
            if (From.Tape == null) return;

            Provider.Effects.Tape.Enabled = From.Tape.Value;
        }
    }
}
