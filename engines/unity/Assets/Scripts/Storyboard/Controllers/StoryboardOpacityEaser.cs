namespace Cytoid.Storyboard.Controllers
{
    public class StoryboardOpacityEaser : StoryboardRendererEaser<ControllerState>
    {
        public StoryboardOpacityEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }
        
        public override void OnUpdate()
        {
            if (From.StoryboardOpacity != null)
            {
                Provider.CanvasGroup.alpha = EaseFloat(From.StoryboardOpacity, To.StoryboardOpacity);
            }
        }
    }
}