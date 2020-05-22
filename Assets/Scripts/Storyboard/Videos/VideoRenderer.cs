using System;
using Cytoid.Storyboard.Sprites;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using static UnityEngine.Object;

namespace Cytoid.Storyboard.Videos
{
    public class VideoRenderer : StageObjectRenderer<Video, VideoState>
    {
        public VideoPlayer VideoPlayer { get; private set; }

        public RawImage RawImage { get; private set; }
        
        public RenderTexture RenderTexture { get; private set; }

        public VideoRenderer(StoryboardRenderer mainRenderer, Video component) : base(mainRenderer, component)
        {
        }

        public override StoryboardRendererEaser<VideoState> CreateEaser() => new VideoEaser(this);

        public override async UniTask Initialize()
        {
            VideoPlayer = Instantiate(Provider.VideoVideoPlayerPrefab);
            RawImage = Instantiate(Provider.VideoRawImagePrefab, Provider.Canvas.transform);
            RenderTexture = new RenderTexture(UnityEngine.Screen.width / 2, UnityEngine.Screen.height / 2, 0, RenderTextureFormat.ARGB32);

            Clear();
            
            var videoPath = Component.States[0].Path;
            if (videoPath == null && Component.States.Count > 1) videoPath = Component.States[1].Path;
            if (videoPath == null)
            {
                throw new InvalidOperationException("Video does not have a valid path");
            }
            VideoPlayer.gameObject.name = RawImage.gameObject.name = $"$Video[{videoPath}";
            
            var path = "file://" + MainRenderer.Game.Level.Path + videoPath;
            VideoPlayer.url = path;
            VideoPlayer.aspectRatio = VideoAspectRatio.FitOutside;
            VideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            VideoPlayer.targetTexture = RenderTexture;
            RawImage.texture = RenderTexture;

            var prepareCompleted = false;
            VideoPlayer.prepareCompleted += _ => prepareCompleted = true;
            VideoPlayer.Prepare();
            await UniTask.WaitUntil(() => prepareCompleted);
        }

        public override void Clear()
        {
            VideoPlayer.Stop();
            RawImage.color = UnityEngine.Color.white.WithAlpha(0);
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
            if (!VideoPlayer.isPlaying)
            {
                var seek = MainRenderer.Time - fromState.Time;
                if (seek > 0)
                {
                    VideoPlayer.time = seek;
                    VideoPlayer.Play();
                }
            }
        }
        
    }
}