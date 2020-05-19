namespace Cytoid.Storyboard.Controllers
{
    public class GlobalNoteRingColorEaser : StoryboardRendererEaser<ControllerState>
    {
        public override void OnUpdate()
        {
            if (From.NoteRingColor.IsSet())
            {
                Game.Config.GlobalRingColorOverride = EaseColor(From.NoteRingColor, To.NoteRingColor);
            }
        }
    }
}