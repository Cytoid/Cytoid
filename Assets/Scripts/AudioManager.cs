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
    private int trackCurrentIndex;
    
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
        }
        else
        {
               
        }
        
        preloadedAudioClips.ForEach(it => Load(it.name, it));
    }

    public Controller Load(string id, AudioClip audioClip)
    {
        if (controllers.ContainsKey(id)) Unload(id);
        return controllers[id] = useNativeAudio ? (Controller) new Exceed7Controller(this, audioClip) : new UnityController(this, audioClip);
    }

    public void Unload(string id)
    {
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
        public abstract void Play(AudioTrackIndex trackIndex);
        public abstract void Pause();
        public abstract void Resume();
        public abstract void Stop();
        public abstract void Unload();
        public abstract bool IsFinished();
    }

    public class UnityController : Controller
    {
        private AudioClip audioClip;
        private int index;

        public UnityController(AudioManager parent, AudioClip audioClip) : base(parent)
        {
            this.audioClip = audioClip;
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
    
        public override void Play(AudioTrackIndex trackIndex)
        {
            index = Parent.GetAvailableIndex(trackIndex);
            Source.Apply(it =>
            {
                it.clip = audioClip;
                it.Play();
            });
        }

        public override void Pause()
        {
            Source.Pause();
        }

        public override void Resume()
        {
            Source.UnPause();
        }

        public override void Stop()
        {
            Source.Stop();
        }

        public override void Unload()
        {
            Stop();
            Source.clip = null;
            audioClip.UnloadAudioData();
            Destroy(audioClip);
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
        private float volume;

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
                controller.SetVolume(volume);
            }
        }

        public override float PlaybackTime
        {
            get => controller.GetPlaybackTime();
            set => controller.SetPlaybackTime(value);
        }

        public override void Play(AudioTrackIndex trackIndex)
        {
            controller = pointer.Play(new NativeAudio.PlayOptions
            {
                audioPlayerIndex = Parent.GetAvailableIndex(trackIndex)
            });
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
            return Mathf.Approximately(PlaybackTime, 0) || Mathf.Approximately(PlaybackTime, length);
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
