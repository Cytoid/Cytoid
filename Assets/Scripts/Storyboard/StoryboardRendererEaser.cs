using UnityEngine;

namespace Cytoid.Storyboard
{
    public abstract class StoryboardRendererEaser<T> where T : ObjectState
    {
        public StoryboardRenderer Renderer { get; set; }
        public StoryboardRendererProvider Provider => StoryboardRendererProvider.Instance;
        public Storyboard Storyboard => Renderer.Storyboard;
        public StoryboardConfig Config => Storyboard.Config;
        public Game Game => Renderer.Game;
        public float Time => Renderer.Time;
        public EasingFunction.Ease Ease { get; set; }
        public T From { get; set; }
        public T To { get; set; }

        public abstract void OnUpdate();

        protected float EaseFloat(float i, float j)
        {
            if (!j.IsSet()) return i;
            if (Time <= From.Time) return i;
            if (Time >= To.Time) return j;
            return EasingFunction
                .GetEasingFunction(Ease)
                .Invoke(i, j, (Time - From.Time) / (To.Time - From.Time));
        }
        
        protected UnityEngine.Color EaseColor(Color i, Color j)
        {
            if (!i.IsSet()) return UnityEngine.Color.clear;
            if (!j.IsSet()) return i.ToUnityColor();
            return UnityEngine.Color.Lerp(i.ToUnityColor(), j.ToUnityColor(), EaseFloat(0, 1));
        }

        protected float EaseCanvasX(float i, float j)
        {
            if (!j.IsSet()) return i;
            return EaseFloat(
                i / StoryboardRenderer.ReferenceWidth * Provider.CanvasRect.width,
                j / StoryboardRenderer.ReferenceWidth * Provider.CanvasRect.width
            );
        }

        protected float EaseCanvasY(float i, float j)
        {
            if (!j.IsSet()) return i;
            return EaseFloat(
                i / StoryboardRenderer.ReferenceHeight * Provider.CanvasRect.height,
                j / StoryboardRenderer.ReferenceHeight * Provider.CanvasRect.height
            );
        }

        protected float EaseOrthographicX(float i, float j)
        {
            if (!j.IsSet()) return i;
            var orthographicSize = Game.camera.orthographicSize;
            return EaseFloat(
                i * orthographicSize / UnityEngine.Screen.height * UnityEngine.Screen.width,
                j * orthographicSize / UnityEngine.Screen.height * UnityEngine.Screen.width
            );
        }

        protected float EaseOrthographicY(float i, float j)
        {
            if (!j.IsSet()) return i;
            var orthographicSize = Game.camera.orthographicSize;
            return EaseFloat(i * orthographicSize, j * orthographicSize);
        }
    }
}