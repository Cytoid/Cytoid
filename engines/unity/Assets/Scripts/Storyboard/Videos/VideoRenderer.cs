using System;
using Cytoid.Storyboard.Sprites;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using static UnityEngine.Object;

namespace Cytoid.Storyboard.Videos
{
    public class VideoRenderer : StoryboardComponentRenderer<Video, VideoState>
    {
        public VideoPlayer VideoPlayer { get; private set; }

        public RawImage RawImage { get; private set; }

        public RenderTexture RenderTexture { get; private set; }

        public RectTransform RectTransform { get; private set; }

        public Canvas Canvas { get; private set; }

        public override Transform Transform => RectTransform;

        public override bool IsOnCanvas => true;

        private bool ownsResources;
        private bool prepareCompleted;
        private VideoPlayer.EventHandler prepareCompletedHandler;

        public VideoRenderer(StoryboardRenderer mainRenderer, Video component) : base(mainRenderer, component)
        {
            prepareCompletedHandler = OnPrepareCompleted;
        }

        public override StoryboardRendererEaser<VideoState> CreateEaser() => new VideoEaser(this);

        public override async UniTask Initialize()
        {
            var version = BeginInitialize();
            var targetRenderer = GetTargetRenderer<VideoRenderer>();
            if (targetRenderer != null)
            {
                ownsResources = false;
                VideoPlayer = targetRenderer.VideoPlayer;
                RawImage = targetRenderer.RawImage;
                RenderTexture = targetRenderer.RenderTexture;
                RectTransform = targetRenderer.RectTransform;
                Canvas = targetRenderer.Canvas;
            }
            else
            {
                ownsResources = true;
                VideoPlayer = Instantiate(Provider.VideoVideoPlayerPrefab);
                RawImage = Instantiate(Provider.VideoRawImagePrefab, Provider.Canvas.transform);
                RenderTexture = new RenderTexture(UnityEngine.Screen.width / 2, UnityEngine.Screen.height / 2, 0, RenderTextureFormat.ARGB32);
                RectTransform = RawImage.rectTransform;
                Canvas = RawImage.GetComponent<Canvas>();
                Canvas.overrideSorting = true;
                Canvas.sortingLayerName = "Storyboard1";

                Clear();

                var videoPath = Component.States[0].Path;
                if (videoPath == null && Component.States.Count > 1) videoPath = Component.States[1].Path;
                if (videoPath == null)
                {
                    throw new InvalidOperationException("Video does not have a valid path");
                }
                VideoPlayer.gameObject.name = RawImage.gameObject.name = $"$Video[{videoPath}]";

                var prefix = "file://";
                if (Application.platform == RuntimePlatform.Android && Context.AndroidVersionCode >= 29)
                {
                    Debug.Log("Detected Android 29 or above. Performing magic...");
                    prefix = ""; // Android Q Unity issue
                    VideoPlayer.source = VideoSource.Url;
                }
                var path = MainRenderer.Game.UsesExternalContent
                    ? GameLaunchVfs.ResolveRequiredFilePath(
                        MainRenderer.Game.Level.Path,
                        videoPath,
                        "storyboard.video.path")
                    : MainRenderer.Game.Level.Path + videoPath;
                path = prefix + path;
                VideoPlayer.url = path;
                VideoPlayer.aspectRatio = VideoAspectRatio.FitOutside;
                VideoPlayer.renderMode = VideoRenderMode.RenderTexture;
                VideoPlayer.targetTexture = RenderTexture;
                RawImage.texture = RenderTexture;

                prepareCompleted = false;
                VideoPlayer.prepareCompleted += prepareCompletedHandler;
                try
                {
                    VideoPlayer.Prepare();
                    var startTime = DateTimeOffset.UtcNow;
                    await UniTask.WaitUntil(() =>
                        prepareCompleted || IsInitializeStale(version) ||
                        DateTimeOffset.UtcNow - startTime > TimeSpan.FromSeconds(5));
                    if (IsInitializeStale(version)) return;
                    if (!prepareCompleted)
                    {
                        Debug.Log($"Android version code: {Context.AndroidVersionCode}");
                        Debug.Log($"Video path: {path}");
                        Debug.LogError("Could not load video. Are you using Android Q or above?");
                    }
                }
                finally
                {
                    UnsubscribePrepareCompleted();
                }
            }
        }

        private void OnPrepareCompleted(VideoPlayer _) => prepareCompleted = true;

        public override void Clear()
        {
            if (ownsResources && VideoPlayer != null)
                VideoPlayer.Stop();
            if (RawImage != null)
                RawImage.color = UnityEngine.Color.white.WithAlpha(0);
            IsTransformActive = false;
        }

        public override void Dispose()
        {
            if (IsDisposed) return;
            UnsubscribePrepareCompleted();
            if (ownsResources)
            {
                if (VideoPlayer != null) Destroy(VideoPlayer.gameObject);
                if (RawImage != null) Destroy(RawImage.gameObject);
                if (RenderTexture != null)
                {
                    RenderTexture.Release();
                    Destroy(RenderTexture);
                }
            }
            VideoPlayer = null;
            RawImage = null;
            RenderTexture = null;
            RectTransform = null;
            Canvas = null;
            ownsResources = false;
            base.Dispose();
        }

        private void UnsubscribePrepareCompleted()
        {
            if (VideoPlayer != null && prepareCompletedHandler != null)
                VideoPlayer.prepareCompleted -= prepareCompletedHandler;
        }

        public override void Update(VideoState fromState, VideoState toState)
        {
            base.Update(fromState, toState);
            SyncPlaybackWithGameState();
        }

        public void SyncPlaybackWithGameState()
        {
            if (VideoPlayer == null || MainRenderer == null || IsDisposed) return;

            if (!MainRenderer.Game.State.IsPlaying)
            {
                if (VideoPlayer.isPlaying)
                {
                    VideoPlayer.Pause();
                }
            }
            else if (!VideoPlayer.isPlaying)
            {
                VideoPlayer.Play();
            }
        }
    }
}
