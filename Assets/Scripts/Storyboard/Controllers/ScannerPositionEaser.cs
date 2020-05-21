namespace Cytoid.Storyboard.Controllers
{
    public class ScannerPositionEaser : StoryboardRendererEaser<ControllerState>
    {
        public ScannerPositionEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }
        
        public override void OnUpdate()
        {
            if (From.OverrideScanlinePos.IsSet())
            {
                Scanner.Instance.positionOverride = From.OverrideScanlinePos.Value
                    ? EaseFloat(From.ScanlinePos, To.ScanlinePos)
                    : float.MinValue;
            }
        }
    }
}