using UnityEngine;

namespace Cytoid.Storyboard.Sprites
{
    public class SpriteEaser : StoryboardRendererEaser<SpriteState>
    {
        public SpriteRenderer SpriteRenderer { get; }
        public UnityEngine.UI.Image Image => SpriteRenderer.Image;
        
        public SpriteEaser(SpriteRenderer renderer) : base(renderer.MainRenderer)
        {
            SpriteRenderer = renderer;
        }

        public override void OnUpdate()
        {
            var rectTransform = Image.rectTransform;

            // X
            if (From.X.IsSet())
            {
                rectTransform.SetLocalX(EaseFloat(From.X, To.X));
            }

            // Y
            if (From.Y.IsSet())
            {
                rectTransform.SetLocalY(EaseFloat(From.Y, To.Y));
            }
            
            // Z
            if (From.Z.IsSet())
            {
                rectTransform.SetLocalZ(EaseFloat(From.Z, To.Z));
            }

            // RotX
            if (From.RotX.IsSet())
            {
                rectTransform.SetLocalEulerAnglesX(EaseFloat(From.RotX, To.RotX));
            }

            // RotY
            if (From.RotY.IsSet())
            {
                rectTransform.SetLocalEulerAnglesY(EaseFloat(From.RotY, To.RotY));
            }

            // RotZ
            if (From.RotZ.IsSet())
            {
                rectTransform.SetLocalEulerAnglesZ(EaseFloat(From.RotZ, To.RotZ));
            }

            // ScaleX
            if (From.ScaleX.IsSet())
            {
                rectTransform.SetLocalScaleX(EaseFloat(From.ScaleX, To.ScaleX));
            }

            // ScaleY
            if (From.ScaleY.IsSet())
            {
                rectTransform.SetLocalScaleY(EaseFloat(From.ScaleY, To.ScaleY));
            }
            
            // PivotX
            if (From.PivotX.IsSet())
            {
                rectTransform.pivot =
                    new Vector2(EaseFloat(From.PivotX, To.PivotX), rectTransform.pivot.y);
            }

            // PivotY
            if (From.PivotY.IsSet())
            {
                rectTransform.pivot =
                    new Vector2(rectTransform.pivot.x, EaseFloat(From.PivotY, To.PivotY));
            }

            // Fill Width
            if (From.FillWidth != null && (bool) From.FillWidth)
            {
                rectTransform.SetWidth(Provider.CanvasRect.width);
                rectTransform.SetHeight(10000);
            }
            else
            {
                // Width
                if (From.Width.IsSet())
                {
                    rectTransform.SetWidth(EaseFloat(From.Width, To.Width));
                }

                // Height
                if (From.Height.IsSet())
                {
                    rectTransform.SetHeight(EaseFloat(From.Height, To.Height));
                }
            }

            // Height
            if (From.Height.IsSet())
            {
                rectTransform.SetHeight(EaseFloat(From.Height, To.Height));
            }

            // Preserve aspect
            if (From.PreserveAspect.IsSet())
            {
                Image.preserveAspect = From.PreserveAspect.Value;
            }

            // Color tint
            if (From.Color.IsSet())
            {
                Image.color = EaseColor(From.Color, To.Color);
            }
            
            // Opacity
            if (From.Opacity.IsSet())
            {
                Image.color = Image.color.WithAlpha(EaseFloat(From.Opacity, To.Opacity));
            }
            
            Canvas canvas = null;

            // Layer
            if (From.Layer.IsSet())
            {
                From.Layer = Mathf.Clamp(From.Layer, 0, 2);
                canvas = Image.GetComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingLayerName = "Storyboard" + (From.Layer + 1);
            }

            // Order
            if (From.Order.IsSet())
            {
                if (canvas == null) canvas = Image.GetComponent<Canvas>();
                canvas.sortingOrder = From.Order;
            }
        }
        
    }
}