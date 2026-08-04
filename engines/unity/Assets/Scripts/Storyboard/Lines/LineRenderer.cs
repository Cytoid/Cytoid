using Cytoid.Storyboard.Lines;
using Cysharp.Threading.Tasks;
using UnityEngine;
using static UnityEngine.Object;

namespace Cytoid.Storyboard.Sprites
{
    public class LineRenderer : StoryboardComponentRenderer<Line, LineState>
    {
        public UnityEngine.LineRenderer Line { get; private set; }

        public override Transform Transform => Line != null ? Line.transform : null;
        
        public override bool IsOnCanvas => false;

        private bool ownsResources;
        
        public LineRenderer(StoryboardRenderer mainRenderer, Line component) : base(mainRenderer, component)
        {
        }

        public override StoryboardRendererEaser<LineState> CreateEaser() => new LineEaser(this);

        public override async UniTask Initialize()
        {
            BeginInitialize();
            var targetRenderer = GetTargetRenderer<LineRenderer>();
            if (targetRenderer != null)
            {
                ownsResources = false;
                Line = targetRenderer.Line;
            }
            else
            {
                ownsResources = true;
                var gameObject = new GameObject("Line_" + Component.Id);
                gameObject.transform.parent = MainRenderer.Game.contentParent.transform;
                Line = gameObject.AddComponent<UnityEngine.LineRenderer>();
                Clear();
            }
        }

        public override void Clear()
        {
            if (Line == null) return;
            Line.positionCount = 0;
            Line.startColor = Line.endColor = UnityEngine.Color.white.WithAlpha(0);
            Line.startWidth = Line.endWidth = 0.05f;
            Line.material = Scanner.Instance.lineRenderer.material;
        }

        public override void Dispose()
        {
            if (IsDisposed) return;
            if (ownsResources && Line != null)
                Destroy(Line.gameObject);
            Line = null;
            ownsResources = false;
            base.Dispose();
        }

    }
}
