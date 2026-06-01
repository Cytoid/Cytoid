namespace Cytoid.Storyboard.Controllers
{
    public class ScannerOpacityEaser : StoryboardRendererEaser<ControllerState>
    {
        public ScannerOpacityEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }
        
        public override void OnUpdate()
        {
            if (From.ScanlineOpacity != null)
            {
                Scanner.Instance.opacity = EaseFloat(From.ScanlineOpacity, To.ScanlineOpacity);
            }
        }
    }
}