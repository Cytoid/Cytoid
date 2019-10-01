namespace Cytoid.Storyboard.Controllers
{
    public class StoryboardOpacityEaser : StoryboardRendererEaser<ControllerState>
    {
        public override void OnUpdate()
        {
            if (From.StoryboardOpacity.IsSet())
            {
                Provider.CanvasGroup.alpha = EaseFloat(From.StoryboardOpacity, To.StoryboardOpacity);
            }
        }
    }
}