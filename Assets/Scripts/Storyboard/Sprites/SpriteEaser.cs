using UnityEngine;

namespace Cytoid.Storyboard.Sprites
{
    public class SpriteEaser : StoryboardRendererEaser<SpriteState>
    {
        public SpriteRenderer SpriteRenderer { get; }
        public UnityEngine.UI.Image Image => SpriteRenderer.Image;
        private RectTransform RectTransform => SpriteRenderer.RectTransform;
        private Canvas Canvas => SpriteRenderer.Canvas;
        private CanvasGroup CanvasGroup => SpriteRenderer.CanvasGroup;
        
        public SpriteEaser(SpriteRenderer renderer) : base(renderer.MainRenderer)
        {
            SpriteRenderer = renderer;
        }

        public override void OnUpdate()
        {
            SpriteRenderer.IsTransformActive = true;
            
            // X
            if (From.X != null)
            {
                RectTransform.SetLocalX(EaseFloat(From.X, To.X));
            }

            // Y
            if (From.Y != null)
            {
                RectTransform.SetLocalY(EaseFloat(From.Y, To.Y));
            }
            
            // Z
            if (From.Z != null)
            {
                RectTransform.SetLocalZ(EaseFloat(From.Z, To.Z));
            }

            // RotX
            if (From.RotX != null)
            {
                RectTransform.SetLocalEulerAnglesX(EaseFloat(From.RotX, To.RotX));
            }

            // RotY
            if (From.RotY != null)
            {
                RectTransform.SetLocalEulerAnglesY(EaseFloat(From.RotY, To.RotY));
            }

            // RotZ
            if (From.RotZ != null)
            {
                RectTransform.SetLocalEulerAnglesZ(EaseFloat(From.RotZ, To.RotZ));
            }

            // ScaleX
            if (From.ScaleX != null)
            {
                RectTransform.SetLocalScaleX(EaseFloat(From.ScaleX, To.ScaleX));
            }

            // ScaleY
            if (From.ScaleY != null)
            {
                RectTransform.SetLocalScaleY(EaseFloat(From.ScaleY, To.ScaleY));
            }
            
            // PivotX
            if (From.PivotX != null)
            {
                RectTransform.pivot =
                    new Vector2(EaseFloat(From.PivotX, To.PivotX), RectTransform.pivot.y);
            }

            // PivotY
            if (From.PivotY != null)
            {
                RectTransform.pivot =
                    new Vector2(RectTransform.pivot.x, EaseFloat(From.PivotY, To.PivotY));
            }

            // Fill Width
            if (From.FillWidth != null && (bool) From.FillWidth)
            {
                RectTransform.SetWidth(Provider.CanvasRect.width);
                RectTransform.SetHeight(10000);
            }
            else
            {
                // Width
                if (From.Width != null)
                {
                    RectTransform.SetWidth(EaseFloat(From.Width, To.Width));
                }

                // Height
                if (From.Height != null)
                {
                    RectTransform.SetHeight(EaseFloat(From.Height, To.Height));
                }
            }

            // Height
            if (From.Height != null)
            {
                RectTransform.SetHeight(EaseFloat(From.Height, To.Height));
            }

            // Preserve aspect
            if (From.PreserveAspect != null)
            {
                Image.preserveAspect = From.PreserveAspect.Value;
            }

            // Color tint
            if (From.Color != null)
            {
                Image.color = EaseColor(From.Color, To.Color);
            }
            
            // Opacity
            if (From.Opacity != null)
            {
                CanvasGroup.alpha = EaseFloat(From.Opacity, To.Opacity);
            }

            // Layer
            if (From.Layer != null)
            {
                From.Layer = Mathf.Clamp(From.Layer.Value, 0, 2);
                Canvas.overrideSorting = true;
                Canvas.sortingLayerName = "Storyboard" + (From.Layer.Value + 1);
            }

            // Order
            if (From.Order != null)
            {
                Canvas.sortingOrder = From.Order.Value;
            }
        }
        
    }
}