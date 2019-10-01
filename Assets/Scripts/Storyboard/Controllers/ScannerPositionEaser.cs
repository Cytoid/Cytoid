namespace Cytoid.Storyboard.Controllers
{
    public class ScannerPositionEaser : StoryboardRendererEaser<ControllerState>
    {
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