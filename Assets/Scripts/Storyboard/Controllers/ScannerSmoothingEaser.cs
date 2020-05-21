namespace Cytoid.Storyboard.Controllers
{
    public class ScannerSmoothingEaser : StoryboardRendererEaser<ControllerState>
    {
        public ScannerSmoothingEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }
        
        public override void OnUpdate()
        {
            if (From.ScanlineSmoothing.IsSet())
            {
                Game.Chart.UseScannerSmoothing = From.ScanlineSmoothing.Value;
            }
        }
    }
}