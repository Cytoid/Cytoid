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
                var easedValue = EaseFloat(From.UiOpacity, To.UiOpacity);
                Game.Renderer.OpacityMultiplier = easedValue;
                
                Provider.UiCanvasGroup.alpha = easedValue;
            }
        }
    }
}