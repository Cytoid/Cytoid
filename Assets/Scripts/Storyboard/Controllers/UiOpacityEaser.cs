namespace Cytoid.Storyboard.Controllers
{
    public class UiOpacityEaser : StoryboardRendererEaser<ControllerState>
    {
        public UiOpacityEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }
        
        public override void OnUpdate()
        {
            if (From.UiOpacity != null)
            {
                if (Game is PlayerGame playerGame && playerGame.HideInterface) return;
                Provider.UiCanvasGroup.alpha = EaseFloat(From.UiOpacity, To.UiOpacity);
            }
        }
    }
}