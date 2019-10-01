namespace Cytoid.Storyboard.Controllers
{
    public class ScannerOpacityEaser : StoryboardRendererEaser<ControllerState>
    {
        public override void OnUpdate()
        {
            if (From.ScanlineOpacity.IsSet())
            {
                Scanner.Instance.opacity = EaseFloat(From.ScanlineOpacity, To.ScanlineOpacity);
            }
        }
    }
}