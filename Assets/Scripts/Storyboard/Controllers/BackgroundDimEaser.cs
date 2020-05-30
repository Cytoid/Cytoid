namespace Cytoid.Storyboard.Controllers
{
    public class BackgroundDimEaser : StoryboardRendererEaser<ControllerState>
    {
        public BackgroundDimEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (From.BackgroundDim != null)
            {
                Provider.Cover.color =
                    Provider.Cover.color.WithAlpha(EaseFloat(1 - From.BackgroundDim, 1 - To.BackgroundDim));
            }
        }
    }
}