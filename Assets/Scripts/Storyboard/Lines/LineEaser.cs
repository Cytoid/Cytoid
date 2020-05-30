using UnityEngine;

namespace Cytoid.Storyboard.Lines
{
    public class LineEaser : StoryboardRendererEaser<LineState>
    {
        public Sprites.LineRenderer LineRenderer { get; }

        public LineRenderer Line => LineRenderer.Line;

        public LineEaser(Sprites.LineRenderer renderer) : base(renderer.MainRenderer)
        {
            LineRenderer = renderer;
        }
        
        public override void OnUpdate()
        {
            // Pos
            Line.positionCount = From.Pos.Count;
            for (var i = 0; i < From.Pos.Count; i++)
            {
                var fromPos = From.Pos[i];
                var toPos = To.Pos.Count > i ? To.Pos[i] : fromPos;
                Line.SetPosition(i, new Vector3(
                    EaseFloat(fromPos.X, toPos.X), 
                    EaseFloat(fromPos.Y, toPos.Y), 
                    EaseFloat(fromPos.Z, toPos.Z)
                ));
            }

            // Width
            if (From.Width != null)
            {
                Line.startWidth = Line.endWidth = EaseFloat(From.Width, To.Width);
            }

            // Color
            if (From.Color != null)
            {
                Line.startColor = Line.endColor = EaseColor(
                    From.Color,
                    To.Color
                );
            }
            
            // Opacity
            if (From.Opacity != null)
            {
                Line.startColor = Line.endColor = Line.startColor.WithAlpha(EaseFloat(From.Opacity, To.Opacity));
            }
            
            // Layer
            if (From.Layer != null)
            {
                From.Layer = Mathf.Clamp(From.Layer.Value, 0, 2);
                Line.sortingLayerName = "Storyboard" + (From.Layer.Value + 1);
            }

            // Order
            if (From.Order != null)
            {
                Line.sortingOrder = From.Order.Value;
            }
        }
    }
}