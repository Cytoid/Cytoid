using System;
using System.Collections.Generic;
using E7.Native;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Assertions;

public class AudioManager : SingletonMonoBehavior<AudioManager>
{
    public AudioSource[] audioSources;
    public AudioClip[] preloadedAudioClips;

    private Dictionary<string, Controller> controllers = new Dictionary<string, Controller>();

    private const int RoundRobinStartIndex = 3;
    private const int RoundRobinEndIndex = 6;
    private int trackCurrentIndex = RoundRobinStartIndex;

    private bool useNativeAudio;

    protected override void Awake()
    {
        base.Awake();
        Context.AudioManager = this;

        Assert.AreEqual(audioSources.Length, 7);
        trackCurrentIndex = RoundRobinStartIndex;
        useNativeAudio = NativeAudio.OnSupportedPlatform();
        if (useNativeAudio)
        {
            NativeAudio.Initialize(new NativeAudio.InitializationOptions
            {
                androidAudioTrackCount = 7
            });
            Debug.Log("Native Audio initialized");
        }
        else
        {
        }

        preloadedAudioClips.ForEach(it => Load(it.name, it));
    }

    public Controller Load(string id, AudioClip audioClip, bool? useNativeAudio = null, bool isResource = false)
    {
        if (useNativeAudio == null) useNativeAudio = this.useNativeAudio;
        if (controllers.ContainsKey(id)) Unload(id);
        print("[AudioManager] Loading " + id);
        return controllers[id] = useNativeAudio.Value
            ? (Controller) new Exceed7Controller(this, audioClip)
            : new UnityController(this, audioClip, isResource);
    }

    public void Unload(string id)
    {
        print("[AudioManager] Unloading " + id);
        controllers[id].Unload();
        controllers.Remove(id);
    }

    public bool IsLoaded(string id) => controllers.ContainsKey(id);

    public Controller Get(string id) => controllers[id];

    private int GetAvailableIndex(AudioTrackIndex trackIndex)
    {
        var index = (int) trackIndex;
        if (index == -1)
        {
            index = trackCurrentIndex;
            trackCurrentIndex++;
            if (trackCurrentIndex > RoundRobinEndIndex)
                trackCurrentIndex = RoundRobinStartIndex;
        }
        return index;
    }

    public abstract class Controller
    {
        protected AudioManager Parent;

        public Controller(AudioManager parent)
        {
            Parent = parent;
        }

        public abstract float Volume { get; set; }
        public abstract float PlaybackTime { get; set; }
        public abstract float Length { get; }
        public abstract void Play(AudioTrackIndex trackIndex = AudioTrackIndex.RoundRobin, bool ignoreDsp = false);
        public abstract double PlayScheduled(AudioTrackIndex trackIndex = AudioTrackIndex.RoundRobin, double delay = 1.0, bool ignoreDsp = false);
        public abstract void Pause();
        public abstract void Resume();
        public abstract void Stop();
        public abstract void Unload();
        public abstract bool IsFinished();
    }

    public class UnityController : Controller
    {
        private AudioClip audioClip;
        private int index = -1;
        private bool isResource;

        public UnityController(AudioManager parent, AudioClip audioClip, bool isResource) : base(parent)
        {
            this.audioClip = audioClip;
            this.isResource = isResource;
        }

        public AudioSource Source => Parent.audioSources[index];

        public override float Volume
        {
            get => Source.volume;
            set => Source.volume = value;
        }

        public override float PlaybackTime
        {
            get => Source.timeSamples * 1f / Source.clip.frequency;
            set => Source.time = value;
        }

        public override float Length => audioClip.length;

        public override void Play(AudioTrackIndex trackIndex = AudioTrackIndex.RoundRobin, bool ignoreDsp = false)
        {
            index = Parent.GetAvailableIndex(trackIndex);
            Source.ignoreListenerPause = ignoreDsp;
            Source.Apply(it =>
            {
                it.clip = audioClip;
                it.Play();
            });
        }

        public override double PlayScheduled(AudioTrackIndex trackIndex = AudioTrackIndex.RoundRobin, double delay = 1.0, bool ignoreDsp = false)
        {
            index = Parent.GetAvailableIndex(trackIndex);
            Source.ignoreListenerPause = ignoreDsp;
            Source.clip = audioClip;
            var time = AudioSettings.dspTime + delay;
            Source.PlayScheduled(time);
            return time;
        }

        public override void Pause()
        {
            if (index < 0) throw new InvalidOperationException();
            Source.Pause();
        }

        public override void Resume()
        {
            if (index < 0) throw new InvalidOperationException();
            Source.UnPause();
        }

        public override void Stop()
        {
            if (index < 0) throw new InvalidOperationException();
            Source.Stop();
        }

        public override void Unload()
        {
            if (index >= 0)
            {
                Stop();
                Source.clip = null;
            }
            audioClip.UnloadAudioData();
            if (!isResource)
            {
                Destroy(audioClip);
            }
            else
            {
                Resources.UnloadAsset(audioClip);
            }
        }

        public override bool IsFinished()
        {
            return !Source.isPlaying;
        }
    }

    public class Exceed7Controller : Controller
    {
        private NativeAudioPointer pointer;
        private NativeAudioController controller;
        private float length;
        private float volume = 1f;

        public Exceed7Controller(AudioManager parent, AudioClip audioClip) : base(parent)
        {
            pointer = NativeAudio.Load(audioClip, NativeAudio.LoadOptions.defaultOptions);
            length = audioClip.length;
        }

        public override float Volume
        {
            get => volume;
            set
            {
                volume = value;
                controller?.SetVolume(volume);
            }
        }

        public override float PlaybackTime
        {
            get => controller.GetPlaybackTime();
            set => controller.SetPlaybackTime(value);
        }

        public override float Length => length;

        public override void Play(AudioTrackIndex trackIndex = AudioTrackIndex.RoundRobin, bool ignoreDsp = false)
        {
            if (ignoreDsp) throw new NotImplementedException();
            controller = pointer.Play(new NativeAudio.PlayOptions
            {
                audioPlayerIndex = Parent.GetAvailableIndex(trackIndex)
            });
            controller.SetVolume(volume);
            Debug.Log("Controller volume set to " + volume);
        }

        public override double PlayScheduled(AudioTrackIndex trackIndex = AudioTrackIndex.RoundRobin, double delay = 1.0, bool ignoreDsp = false)
        {
            throw new NotImplementedException();
        }

        public override void Pause()
        {
            controller.TrackPause();
        }

        public override void Resume()
        {
            controller.TrackResume();
        }

        public override void Stop()
        {
            controller.Stop();
        }

        public override async void Unload()
        {
            Stop();
            await UniTask.DelayFrame(10);
            pointer.Unload();
        }

        public override bool IsFinished()
        {
            print("Playback time: " + PlaybackTime + ", Length: " + length);
            return Mathf.Approximately(PlaybackTime, 0) || PlaybackTime >= length;
        }

    }
}

public enum AudioTrackIndex
{
    Reserved1 = 0,
    Reserved2 = 1,
    Reserved3 = 2,
    RoundRobin = -1
}