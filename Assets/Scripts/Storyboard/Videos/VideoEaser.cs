using Cytoid.Storyboard.Videos;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Cytoid.Storyboard.Sprites
{
    public class VideoEaser : StoryboardRendererEaser<VideoState>
    {
        public VideoRenderer VideoRenderer { get; }
        public VideoPlayer VideoPlayer => VideoRenderer.VideoPlayer;
        public RawImage RawImage => VideoRenderer.RawImage;
        
        public VideoEaser(VideoRenderer renderer) : base(renderer.MainRenderer)
        {
            VideoRenderer = renderer;
        }

        public override void OnUpdate()
        {
            var rectTransform = RawImage.rectTransform;

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

            // Color tint
            if (From.Color.IsSet())
            {
                RawImage.color = EaseColor(From.Color, To.Color);
            }
            
            // Opacity
            if (From.Opacity.IsSet())
            {
                RawImage.color = RawImage.color.WithAlpha(EaseFloat(From.Opacity, To.Opacity));
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

            Canvas canvas = null;

            // Layer
            if (From.Layer.IsSet())
            {
                From.Layer = Mathf.Clamp(From.Layer, 0, 2);
                canvas = RawImage.GetComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingLayerName = "Storyboard" + (From.Layer + 1);
            }

            // Order
            if (From.Order.IsSet())
            {
                if (canvas == null) canvas = RawImage.GetComponent<Canvas>();
                canvas.sortingOrder = From.Order;
            }
        }
        
    }
}