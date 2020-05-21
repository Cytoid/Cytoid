using Cytoid.Storyboard.Notes;
using UniRx.Async;

namespace Cytoid.Storyboard.Sprites
{
    public class NoteControllerRenderer : StoryboardComponentRenderer<NoteController, NoteControllerState>
    {

        public NoteControllerRenderer(StoryboardRenderer mainRenderer, NoteController component) : base(mainRenderer, component)
        {
        }

        public override StoryboardRendererEaser<NoteControllerState> CreateEaser() => new NoteControllerEaser(MainRenderer);

        public override async UniTask Initialize()
        {
        }

        public override void Clear()
        {;
        }

        public override void Dispose()
        {
        }

    }
}