using System;
using UnityEngine;

namespace Cytoid.Storyboard.Texts
{
    public class TextEaser : StoryboardRendererEaser<TextState>
    {
        private TextRenderer TextRenderer { get; }
        private UnityEngine.UI.Text Text => TextRenderer.Text;
        private RectTransform RectTransform => TextRenderer.RectTransform;
        private Canvas Canvas => TextRenderer.Canvas;
        private CanvasGroup CanvasGroup => TextRenderer.CanvasGroup;
        
        public TextEaser(TextRenderer renderer) : base(renderer.MainRenderer)
        {
            TextRenderer = renderer;
        }

        public override void OnUpdate()
        {
            TextRenderer.IsTransformActive = true;
            
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

            // RotX
            if (From.RotX != null)
            {
                RectTransform.localEulerAngles =
                    RectTransform.localEulerAngles.SetX(EaseFloat(From.RotX, To.RotX));
            }

            // RotY
            if (From.RotY != null)
            {
                RectTransform.localEulerAngles =
                    RectTransform.localEulerAngles.SetY(EaseFloat(From.RotY, To.RotY));
            }

            // RotZ
            if (From.RotZ != null)
            {
                RectTransform.localEulerAngles =
                    RectTransform.localEulerAngles.SetZ(EaseFloat(From.RotZ, To.RotZ));
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

            // Color
            if (From.Color != null)
            {
                Text.color = EaseColor(From.Color, To.Color);
            }

            // Opacity
            if (From.Opacity != null)
            {
                CanvasGroup.alpha = EaseFloat(From.Opacity, To.Opacity);
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
            if (From.FillWidth != null && From.FillWidth.Value)
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

            // Text
            if (From.Text != null)
            {
                Text.text = From.Text;
            }

            // Size
            if (From.Size != null)
            {
                Text.fontSize = From.Size.Value;
            }

            // Align
            if (From.Align != null)
            {
                Text.alignment =
                    (TextAnchor) Enum.Parse(typeof(TextAnchor), From.Align, true);
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