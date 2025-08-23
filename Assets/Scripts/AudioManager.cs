using System;
using System.Collections.Generic;
using System.Linq;
using E7.Native;
using Cysharp.Threading.Tasks;
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
    private bool isInitialized;

    protected override void Awake()
    {
        base.Awake();
        Context.AudioManager = this;
        Assert.AreEqual(audioSources.Length, 7);
        trackCurrentIndex = RoundRobinStartIndex;
    }

    public void Initialize()
    {
        if (isInitialized) return;
        isInitialized = true;
        SetUseNativeAudio(Context.Player.Settings.UseNativeAudio);
        if (useNativeAudio)
        {
            var options = new NativeAudio.InitializationOptions
            {
                androidAudioTrackCount = 2,
                androidBufferSize = -1 // 使用设备原生缓冲区大小
            };
            NativeAudio.Initialize(options);
            Debug.Log($"Native Audio initialized with {NativeAudio.GetNativeSourceCount()} sources");
        }

        preloadedAudioClips.ForEach(it => Load(it.name, it, isPreloaded: true));
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispose();
    }

    public void Dispose()
    {
        if (!isInitialized) return;

        isInitialized = false;

        // First stop all playing audio
        foreach (var controller in controllers.Values)
        {
            try
            {
                controller.Stop();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error stopping audio controller: {e}");
            }
        }

        // Then unload non-preloaded audio
        var keysToUnload = controllers.Keys.ToList().FindAll(it => !controllers[it].IsPreloaded);
        foreach (var key in keysToUnload)
        {
            try
            {
                Unload(key);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error unloading audio {key}: {e}");
            }
        }

        // Clear controllers dictionary
        controllers.Clear();

        // Finally dispose native audio
        try
        {
            NativeAudio.Dispose();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error disposing native audio: {e}");
        }
    }

    public void SetUseNativeAudio(bool useNativeAudio)
    {
        this.useNativeAudio = NativeAudio.OnSupportedPlatform && useNativeAudio;
    }

    public Controller Load(string id, AudioClip audioClip, bool? useNativeAudio = null, bool isResource = false, bool isMusic = false, bool isPreloaded = false)
    {
        if (useNativeAudio == null) useNativeAudio = this.useNativeAudio;
        if (controllers.ContainsKey(id)) Unload(id);
        print("[AudioManager] Loading " + id);
        return controllers[id] = useNativeAudio.Value
            ? (Controller)new Exceed7Controller(this, audioClip, isMusic, isPreloaded)
            : new UnityController(this, audioClip, isResource, isMusic, isPreloaded);
    }

    public void Unload(string id)
    {
        if (controllers.ContainsKey(id))
        {
            print("[AudioManager] Unloading " + id);
            controllers[id].Unload();
            controllers.Remove(id);
        }
    }

    public bool IsLoaded(string id) => controllers.ContainsKey(id);

    public Controller Get(string id) => controllers[id];

    public void UpdateVolumes()
    {
        foreach (var controller in controllers.Values)
        {
            controller.Volume = controller.IsMusic ? Context.Player.Settings.MusicVolume : Context.Player.Settings.SoundEffectsVolume;
        }
    }

    private int GetAvailableIndex(AudioTrackIndex trackIndex)
    {
        var index = (int)trackIndex;
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
        public bool IsMusic { get; }
        public bool IsPreloaded { get; }

        public Controller(AudioManager parent, bool isMusic, bool isPreloaded)
        {
            Parent = parent;
            IsMusic = isMusic;
            IsPreloaded = isPreloaded;
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
        public abstract bool IsPlaying();
    }

    public class UnityController : Controller
    {
        private AudioClip audioClip;
        private int index = -1;
        private bool isResource;
        private float volume = 1f;

        public UnityController(AudioManager parent, AudioClip audioClip, bool isResource, bool isMusic, bool isPreloaded) : base(parent, isMusic, isPreloaded)
        {
            this.audioClip = audioClip;
            this.isResource = isResource;
            volume = isMusic ? Context.Player.Settings.MusicVolume : Context.Player.Settings.SoundEffectsVolume;
        }

        public AudioSource Source => Parent.audioSources[index];

        public override float Volume
        {
            get => volume;
            set
            {
                volume = value;
                if (index >= 0) Source.volume = volume;
            }
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
                it.volume = Volume;
                it.Play();
            });
        }

        public override double PlayScheduled(AudioTrackIndex trackIndex = AudioTrackIndex.RoundRobin, double delay = 1.0, bool ignoreDsp = false)
        {
            index = Parent.GetAvailableIndex(trackIndex);
            Source.ignoreListenerPause = ignoreDsp;
            Source.clip = audioClip;
            Source.volume = Volume;
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

        public override bool IsPlaying()
        {
            return Source.isPlaying;
        }
    }

    public class Exceed7Controller : Controller
    {
        private NativeAudioPointer pointer;
        private NativeSource source;
        private float length;
        private float volume = 1f;
        private bool isPlaying = false;

        public Exceed7Controller(AudioManager parent, AudioClip audioClip, bool isMusic, bool isPreloaded) : base(parent, isMusic, isPreloaded)
        {
            pointer = NativeAudio.Load(audioClip, NativeAudio.LoadOptions.defaultOptions);
            length = audioClip.length;
            volume = isMusic ? Context.Player.Settings.MusicVolume : Context.Player.Settings.SoundEffectsVolume;
        }

        public override float Volume
        {
            get => volume;
            set
            {
                volume = value;
                if (source.IsValid)
                {
                    source.SetVolume(volume);
                }
            }
        }

        public override float PlaybackTime
        {
            get => source.IsValid ? source.GetPlaybackTime() : 0f;
            set
            {
                if (source.IsValid)
                {
                    source.SetPlaybackTime(value);
                }
            }
        }

        public override float Length => length;

        public override void Play(AudioTrackIndex trackIndex = AudioTrackIndex.RoundRobin, bool ignoreDsp = false)
        {
            var sourceIndex = Parent.GetAvailableIndex(trackIndex);
            source = NativeAudio.GetNativeSource(sourceIndex);
            source.Play(pointer);
#if UNITY_ANDROID
            source.SetVolume(volume >= 0.05f ? volume : float.Epsilon);
#else
            source.SetVolume(volume);
#endif
            isPlaying = true;
            Debug.Log("Source volume set to " + volume);
        }

        public override double PlayScheduled(AudioTrackIndex trackIndex = AudioTrackIndex.RoundRobin, double delay = 1.0, bool ignoreDsp = false)
        {
            throw new NotImplementedException();
        }

        public override void Pause()
        {
            isPlaying = false;
            if (source.IsValid)
            {
                source.Pause();
            }
        }

        public override void Resume()
        {
            isPlaying = true;
            if (source.IsValid)
            {
                source.Resume();
            }
        }

        public override void Stop()
        {
            isPlaying = false;
            if (source.IsValid)
            {
                source.Stop();
            }
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

        public override bool IsPlaying()
        {
            return isPlaying;
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
