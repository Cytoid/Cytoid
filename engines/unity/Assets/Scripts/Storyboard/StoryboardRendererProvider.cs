using Cytoid.Storyboard.PostProcess;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Cytoid.Storyboard
{
    public class StoryboardRendererProvider : SingletonMonoBehavior<StoryboardRendererProvider>
    {
        static readonly StoryboardEffectsChannels NullChannels = new();

        public Camera Camera;
        public Image Cover;
        public Canvas Canvas;
        public CanvasGroup CanvasGroup;
        public RectTransform CanvasRectTransform;
        public Rect CanvasRect => CanvasRectTransform.rect;

        public CanvasGroup UiCanvasGroup;

        public StoryboardEffectsChannels Effects =>
            StoryboardEffects.Current?.Channels ?? NullChannels;

        public UnityEngine.UI.Text TextPrefab;
        public Image SpritePrefab;
        public VideoPlayer VideoVideoPlayerPrefab;
        public RawImage VideoRawImagePrefab;
    }
}
