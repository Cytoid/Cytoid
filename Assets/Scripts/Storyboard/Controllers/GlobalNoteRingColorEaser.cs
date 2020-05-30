namespace Cytoid.Storyboard.Controllers
{
    public class GlobalNoteRingColorEaser : StoryboardRendererEaser<ControllerState>
    {
        public GlobalNoteRingColorEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }
        
        public override void OnUpdate()
        {
            if (From.NoteRingColor != null)
            {
                Game.Config.GlobalRingColorOverride = EaseColor(From.NoteRingColor, To.NoteRingColor);
            }
        }
    }
}