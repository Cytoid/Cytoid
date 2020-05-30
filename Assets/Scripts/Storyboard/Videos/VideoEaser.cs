using Cytoid.Storyboard.Videos;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Cytoid.Storyboard.Sprites
{
    public class VideoEaser : StoryboardRendererEaser<VideoState>
    {
        private VideoRenderer VideoRenderer { get; }
        private VideoPlayer VideoPlayer => VideoRenderer.VideoPlayer;
        private RawImage RawImage => VideoRenderer.RawImage;
        private RectTransform RectTransform => VideoRenderer.RectTransform;
        private Canvas Canvas => VideoRenderer.Canvas;
        
        public VideoEaser(VideoRenderer renderer) : base(renderer.MainRenderer)
        {
            VideoRenderer = renderer;
        }

        public override void OnUpdate()
        {
            VideoRenderer.IsTransformActive = true;
            
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

            // Color tint
            if (From.Color != null)
            {
                RawImage.color = EaseColor(From.Color, To.Color);
            }
            
            // Opacity
            if (From.Opacity != null)
            {
                RawImage.color = RawImage.color.WithAlpha(EaseFloat(From.Opacity, To.Opacity));
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

            // Height
            if (From.Height != null)
            {
                RectTransform.SetHeight(EaseFloat(From.Height, To.Height));
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