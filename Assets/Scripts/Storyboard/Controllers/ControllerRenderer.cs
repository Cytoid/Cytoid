using Cytoid.Storyboard.Lines;
using Storyboard.Controllers;
using UniRx.Async;
using UnityEngine;
using static UnityEngine.Object;

namespace Cytoid.Storyboard.Controllers
{
    public class ControllerRenderer : StoryboardComponentRenderer<Controller, ControllerState>
    {

        public ControllerRenderer(StoryboardRenderer mainRenderer, Controller component) : base(mainRenderer, component)
        {
        }

        public override StoryboardRendererEaser<ControllerState> CreateEaser() => new ControllerEaser(MainRenderer);

        public override async UniTask Initialize()
        {
        }

        public override void Clear()
        {
        }

        public override void Dispose()
        {
        }

    }
}