using System;
using UnityEngine;

namespace Cytoid.Storyboard
{
    public abstract class StoryboardRendererEaser<T> where T : ObjectState
    {
        public StoryboardRenderer Renderer { get; }
        public float Time { get; set; }
        
        public StoryboardRendererProvider Provider => StoryboardRendererProvider.Instance;
        public Storyboard Storyboard => Renderer.Storyboard;
        public StoryboardConfig Config => Storyboard.Config;
        public Game Game => Renderer.Game;
        public EasingFunction.Ease Ease { get; set; }
        public T From { get; set; }
        public T To { get; set; }

        public StoryboardRendererEaser(StoryboardRenderer renderer)
        {
            Renderer = renderer;
        }

        public abstract void OnUpdate();

        protected float EaseFloat(float? i, float? j)
        {
            if (i == null) throw new ArgumentNullException();
            if (j == null) return i.Value;
            if (Time <= From.Time) return i.Value;
            if (Time >= To.Time) return j.Value;
            return EasingFunction
                .GetEasingFunction(Ease)
                .Invoke(i.Value, j.Value, (Time - From.Time) / (To.Time - From.Time));
        }
        
        protected float EaseFloat(UnitFloat i, UnitFloat j)
        {
            if (i == null) throw new ArgumentNullException();
            if (j == null) return i.ConvertedValue;
            if (Time <= From.Time) return i.ConvertedValue;
            if (Time >= To.Time) return j.ConvertedValue; 
            return EasingFunction
                .GetEasingFunction(Ease)
                .Invoke(i.ConvertedValue, j.ConvertedValue, (Time - From.Time) / (To.Time - From.Time));
        }
        
        protected UnityEngine.Color EaseColor(Color i, Color j)
        {
            if (i == null) return UnityEngine.Color.clear;
            if (j == null) return i.ToUnityColor();
            return UnityEngine.Color.Lerp(i.ToUnityColor(), j.ToUnityColor(), EaseFloat(0, 1));
        }
        
    }
}