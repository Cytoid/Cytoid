using System;
using UnityEngine;

namespace Cytoid.Storyboard.Texts
{
    public class TextEaser : StoryboardRendererEaser<TextState>
    {
        public TextRenderer TextRenderer { get; }
        public UnityEngine.UI.Text Text => TextRenderer.Text;
        
        public TextEaser(TextRenderer renderer) : base(renderer.MainRenderer)
        {
            TextRenderer = renderer;
        }

        public override void OnUpdate()
        {
            var rectTransform = Text.rectTransform;

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

            // RotX
            if (From.RotX.IsSet())
            {
                rectTransform.localEulerAngles =
                    rectTransform.localEulerAngles.SetX(EaseFloat(From.RotX, To.RotX));
            }

            // RotY
            if (From.RotY.IsSet())
            {
                rectTransform.localEulerAngles =
                    rectTransform.localEulerAngles.SetY(EaseFloat(From.RotY, To.RotY));
            }

            // RotZ
            if (From.RotZ.IsSet())
            {
                rectTransform.localEulerAngles =
                    rectTransform.localEulerAngles.SetZ(EaseFloat(From.RotZ, To.RotZ));
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

            // Color
            if (From.Color.IsSet())
            {
                Text.color = EaseColor(From.Color, To.Color);
            }

            // Opacity
            if (From.Opacity.IsSet())
            {
                Text.color = Text.color.WithAlpha(EaseFloat(From.Opacity, To.Opacity));
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
            if (From.FillWidth.IsSet() && From.FillWidth.Value)
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

            // Text
            if (From.Text.IsSet())
            {
                Text.text = From.Text;
            }

            // Size
            if (From.Size.IsSet())
            {
                Text.fontSize = From.Size;
            }

            // Align
            if (From.Align.IsSet())
            {
                Text.alignment =
                    (TextAnchor) Enum.Parse(typeof(TextAnchor), From.Align, true);
            }

            Canvas canvas = null;

            // Layer
            if (From.Layer.IsSet())
            {
                From.Layer = Mathf.Clamp(From.Layer, 0, 2);
                canvas = Text.GetComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingLayerName = "Storyboard" + (From.Layer + 1);
            }

            // Order
            if (From.Order.IsSet())
            {
                if (canvas == null) canvas = Text.GetComponent<Canvas>();
                canvas.sortingOrder = From.Order;
            }
        }
        
    }
}