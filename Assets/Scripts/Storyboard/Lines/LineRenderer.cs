using Cytoid.Storyboard.Lines;
using UniRx.Async;
using UnityEngine;
using static UnityEngine.Object;

namespace Cytoid.Storyboard.Sprites
{
    public class LineRenderer : StageObjectRenderer<Line, LineState>
    {
        public UnityEngine.LineRenderer Line { get; private set; }

        public LineRenderer(StoryboardRenderer mainRenderer, Line component) : base(mainRenderer, component)
        {
        }

        public override StoryboardRendererEaser<LineState> CreateEaser() => new LineEaser(this);

        public override async UniTask Initialize()
        {
            var gameObject = new GameObject("Line_" + Component.Id);
            gameObject.transform.parent = MainRenderer.Game.contentParent.transform;
            Line = gameObject.AddComponent<UnityEngine.LineRenderer>();
            Clear();
        }

        public override void Clear()
        {
            Line.positionCount = 0;
            Line.startColor = Line.endColor = UnityEngine.Color.white.WithAlpha(0);
            Line.startWidth = Line.endWidth = 0.05f;
            Line.material = Scanner.Instance.lineRenderer.material;
        }

        public override void Dispose()
        { 
            Destroy(Line.gameObject);
        }

    }
}