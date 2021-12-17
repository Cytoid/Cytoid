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
        
        public VideoRenderer(StoryboardRenderer mainRenderer, Video component) : base(mainRenderer, component)
        {
        }

        public override StoryboardRendererEaser<VideoState> CreateEaser() => new VideoEaser(this);

        public override async UniTask Initialize()
        {
            var targetRenderer = GetTargetRenderer<VideoRenderer>();
            if (targetRenderer != null)
            {
                VideoPlayer = targetRenderer.VideoPlayer;
                RawImage = targetRenderer.RawImage;
                RenderTexture = targetRenderer.RenderTexture;
                RectTransform = targetRenderer.RectTransform;
                Canvas = targetRenderer.Canvas;
            }
            else
            {
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
                var path = prefix + MainRenderer.Game.Level.Path + videoPath;
                VideoPlayer.url = path;
                VideoPlayer.aspectRatio = VideoAspectRatio.FitOutside;
                VideoPlayer.renderMode = VideoRenderMode.RenderTexture;
                VideoPlayer.targetTexture = RenderTexture;
                RawImage.texture = RenderTexture;

                var prepareCompleted = false;
                VideoPlayer.prepareCompleted += _ => prepareCompleted = true;
                VideoPlayer.Prepare();
                var startTime = DateTimeOffset.UtcNow;
                await UniTask.WaitUntil(() => prepareCompleted || DateTimeOffset.UtcNow - startTime > TimeSpan.FromSeconds(5));
                if (!prepareCompleted)
                {
                    Debug.Log($"Android version code: {Context.AndroidVersionCode}");
                    Debug.Log($"Video path: {path}");
                    Debug.LogError("Could not load video. Are you using Android Q or above?");
                }
            }
        }

        public override void Clear()
        {
            VideoPlayer.Stop();
            RawImage.color = UnityEngine.Color.white.WithAlpha(0);
            IsTransformActive = false;
        }

        public override void Dispose()
        {
            Destroy(VideoPlayer.gameObject);
            Destroy(RawImage.gameObject);
            Destroy(RenderTexture);
        }
        
        public override void Update(VideoState fromState, VideoState toState)
        {
            base.Update(fromState, toState);
            if (!MainRenderer.Game.State.IsPlaying)
            {
                if (VideoPlayer.isPlaying)
                {
                    VideoPlayer.Pause();
                }
            }
            else
            {
                if (!VideoPlayer.isPlaying)
                {
                    VideoPlayer.Play();
                    // var seek = MainRenderer.Time - fromState.Time;
                    // if (seek > 0)
                    // {
                    //     VideoPlayer.time = seek;
                    //     VideoPlayer.Play();
                    // }
                }
            }
        }
        
    }
}