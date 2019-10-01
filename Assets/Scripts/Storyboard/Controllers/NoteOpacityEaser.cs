namespace Cytoid.Storyboard.Controllers
{
    public class NoteOpacityEaser : StoryboardRendererEaser<ControllerState>
    {
        public override void OnUpdate()
        {
            if (From.NoteOpacityMultiplier.IsSet())
            {
                Game.Config.GlobalNoteOpacityMultiplier = EaseFloat(From.NoteOpacityMultiplier, To.NoteOpacityMultiplier);
            }
        }
    }
}