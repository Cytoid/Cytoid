using UnityEngine;

namespace Cytoid.Storyboard
{
    public abstract class StoryboardRendererEaser<T> where T : ObjectState
    {
        public StoryboardRenderer Renderer { get; }
        public StoryboardRendererProvider Provider => StoryboardRendererProvider.Instance;
        public Storyboard Storyboard => Renderer.Storyboard;
        public StoryboardConfig Config => Storyboard.Config;
        public Game Game => Renderer.Game;
        public float Time => Renderer.Time;
        public EasingFunction.Ease Ease { get; set; }
        public T From { get; set; }
        public T To { get; set; }

        public StoryboardRendererEaser(StoryboardRenderer renderer)
        {
            Renderer = renderer;
        }

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
        
    }
}