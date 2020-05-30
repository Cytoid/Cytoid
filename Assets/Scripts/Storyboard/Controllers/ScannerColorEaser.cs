namespace Cytoid.Storyboard.Controllers
{
    public class ScannerColorEaser : StoryboardRendererEaser<ControllerState>
    {
        public ScannerColorEaser(StoryboardRenderer renderer) : base(renderer)
        {
            
        }
        
        public override void OnUpdate()
        {
            if (From.ScanlineColor != null)
            {
                Scanner.Instance.colorOverride = EaseColor(From.ScanlineColor, To.ScanlineColor);
            }
        }
    }
}