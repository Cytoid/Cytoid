namespace Cytoid.Storyboard.Controllers
{
    public class NoteOpacityEaser : StoryboardRendererEaser<ControllerState>
    {
        public NoteOpacityEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }
        
        public override void OnUpdate()
        {
            if (From.NoteOpacityMultiplier != null)
            {
                Game.Config.GlobalNoteOpacityMultiplier = EaseFloat(From.NoteOpacityMultiplier, To.NoteOpacityMultiplier);
            }
        }
    }
}