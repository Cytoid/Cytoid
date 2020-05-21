using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Cytoid.Storyboard
{

    [Serializable]
    public abstract class Object
    {
        public string Id;

        public abstract List<ObjectState> GetConcreteStates(); // Quite ugly, but ¯\_(ツ)_/¯

        public abstract bool IsManuallySpawned();
    }
    
    [Serializable]
    public class Object<T> : Object where T : ObjectState
    {
        public List<T> States = new List<T>();

        public override List<ObjectState> GetConcreteStates()
        {
            return new List<ObjectState>(States);
        }

        public override bool IsManuallySpawned()
        {
            return States[0].Time == float.MaxValue;
        }
    }

    [Serializable]
    public class Text : Object<TextState>
    {
    }

    [Serializable]
    public class Sprite : Object<SpriteState>
    {
    }

    [Serializable]
    public class Video : Object<VideoState>
    {
    }

    [Serializable]
    public class Controller : Object<ControllerState>
    {
    }

    [Serializable]
    public class NoteController : Object<NoteControllerState>
    {
    }
    
    [Serializable]
    public class Line : Object<LineState>
    {
    }

    [Serializable]
    public class ObjectState
    {
        public float AddTime = float.MinValue;
        public bool Destroy;
        public EasingFunction.Ease Easing;

        public float RelativeTime = float.MinValue;

        // If time is not defined, this object is never rendered - unless cloned and recalculated by a trigger
        public float Time = float.MaxValue;
    }

    [Serializable]
    public class CanvasObjectState : ObjectState
    {
        public bool? FillWidth;
        public float Height = float.MinValue;

        public int Layer = int.MinValue;

        public float Opacity = float.MinValue;
        public int Order = int.MinValue;

        public float PivotX = float.MinValue;
        public float PivotY = float.MinValue;
        public float RotX = float.MinValue;
        public float RotY = float.MinValue;
        public float RotZ = float.MinValue;

        public float ScaleX = float.MinValue;
        public float ScaleY = float.MinValue;

        public float Width = float.MinValue;
        public float X = float.MinValue;
        public float Y = float.MinValue;
    }

    [Serializable]
    public class TextState : CanvasObjectState
    {
        public string Align;
        public Color Color;
        public string Font;
        public int Size = int.MinValue;
        public string Text;
    }

    [Serializable]
    public class SpriteState : CanvasObjectState
    {
        public Color Color;
        public string Path;
        public bool? PreserveAspect;
    }

    [Serializable]
    public class VideoState : CanvasObjectState
    {
        public Color Color;
        public string Path;
    }

    [Serializable]
    public class NoteSelector
    {
        public HashSet<int> Types = new HashSet<int>();
        public int Start = int.MinValue;
        public int End = int.MaxValue;
    }

    [Serializable]
    public class NoteControllerState : ObjectState
    {
        public int? Note;
        public bool? OverrideX;
        public float X = float.MinValue;
        public bool? OverrideY;
        public float Y = float.MinValue;
        public float Rot = float.MinValue;
        public bool? OverrideRingColor;
        public Color RingColor;
        public bool? OverrideFillColor;
        public Color FillColor;
        public float OpacityMultiplier = float.MinValue;
        public float SizeMultiplier = float.MinValue;
        public int HoldDirection = int.MinValue;
        public int Style = int.MinValue;
    }

    [Serializable]
    public class LinePosition
    {
        public float X = float.MinValue;
        public float Y = float.MinValue;
    }

    [Serializable]
    public class LineState : ObjectState
    {
        public List<LinePosition> Pos = new List<LinePosition>();
        public float Width = float.MinValue;
        public Color Color;
        public float Opacity = float.MinValue;
        public int Layer = int.MinValue;
        public int Order = int.MinValue;
    }

    [Serializable]
    public class ControllerState : ObjectState
    {
        public bool? Arcade;
        public float ArcadeContrast = float.MinValue; // Range: 0~10, Default: 1
        public float ArcadeIntensity = float.MinValue; // Range: 0~1
        public float ArcadeInterferanceSize = float.MinValue; // Range: 0~10, Default: 1
        public float ArcadeInterferanceSpeed = float.MinValue; // Range: 0~10, Default: 0.5

        public bool? Artifact;
        public float ArtifactColorisation = float.MinValue; // Range: -10~10, Default: 1
        public float ArtifactIntensity = float.MinValue; // Range: 0~1
        public float ArtifactNoise = float.MinValue; // Range: -10~10, Default: 1
        public float ArtifactParasite = float.MinValue; // Range: -10~10, Default: 1
        public float BackgroundDim = float.MinValue;

        public bool? Bloom;
        public float BloomIntensity = float.MinValue; // Range: 0~5
        public float Brightness = float.MinValue; // Range: 0~10, Default: 1

        public bool? Chromatic;

        public bool? Chromatical;
        public float ChromaticalFade = float.MinValue; // Range: 0~1
        public float ChromaticalIntensity = float.MinValue; // Range: 0~1
        public float ChromaticalSpeed = float.MinValue; // Range: 0~3
        public float ChromaticEnd = float.MinValue; // Range: 0~1
        public float ChromaticIntensity = float.MinValue; // Range: 0~0.15
        public float ChromaticStart = float.MinValue; // Range: 0~1

        public bool? ColorAdjustment;

        public bool? ColorFilter;
        public Color ColorFilterColor;
        public float Contrast = float.MinValue; // Range: 0~10, Default: 1

        public bool? Dream;
        public float DreamIntensity = float.MinValue; // Range: 1~10

        public bool? Fisheye;
        public float FisheyeIntensity = float.MinValue; // Range: 0~1, Default: 0.5

        public bool? Focus;
        public Color FocusColor;
        public float FocusIntensity = float.MinValue; // Range: 0~1, Default: 0.25
        public float FocusSize = float.MinValue; // Range: 1~10, Default: 1
        public float FocusSpeed = float.MinValue; // Range: 0~30, Default: 5
        public float Fov = float.MinValue; // Field of View Default: 53.2

        public bool? Glitch;
        public float GlitchIntensity = float.MinValue; // Range: 0~1

        public bool? GrayScale;
        public float GrayScaleIntensity = float.MinValue; // Range: 0~1

        public bool? Noise;
        public float NoiseIntensity = float.MinValue; // Range: 0~1, Default: 0.235
        public List<Color> NoteFillColors;
        public float NoteOpacityMultiplier = float.MinValue;
        public Color NoteRingColor;
        public bool? OverrideScanlinePos;
        public bool? Perspective;

        public bool? RadialBlur;
        public float RadialBlurIntensity = float.MinValue; // Range: -0.5~0.5, Default: 0.025
        public float RotX = float.MinValue;
        public float RotY = float.MinValue;
        public float RotZ = float.MinValue;
        public float Saturation = float.MinValue; // Range: 0~10, Default: 1

        public Color ScanlineColor;
        public float ScanlineOpacity = float.MinValue;
        public float ScanlinePos = float.MinValue;
        public bool? ScanlineSmoothing;

        public bool? Sepia;
        public float SepiaIntensity = float.MinValue; // Range: 0~1

        public bool? Shockwave;
        public float ShockwaveSpeed = float.MinValue; // Range: 0~10, Default: 1

        public float Size = float.MinValue; // Camera.main.orthographicSize Default: 5
        public float StoryboardOpacity = float.MinValue;

        public bool? Tape;
        public float UiOpacity = float.MinValue;

        public bool? Vignette;

        public Color VignetteColor;
        public float VignetteEnd = float.MinValue; // Range: 0~1
        public float VignetteIntensity = float.MinValue; // Range: 0~1
        public float VignetteStart = float.MinValue; // Range: 0~1
        public float X = float.MinValue; // Every x/y = 2 * Camera.main.orthographicSize
        public float Y = float.MinValue;
    }

    [Serializable]
    public class Trigger
    {
        public int Combo = int.MinValue;
        [JsonIgnore] public int CurrentUses;
        public List<string> Destroy = new List<string>();

        public List<int> Notes = new List<int>();
        public int Score = int.MinValue;
        public List<string> Spawn = new List<string>();

        [JsonIgnore] public Note Triggerer;
        public TriggerType Type = TriggerType.None;
        public int Uses = int.MinValue;
    }

    public enum TriggerType
    {
        NoteClear,
        Combo,
        Score,
        None
    }

    [Serializable]
    public class Color
    {
        public float A = 1;
        public float B = 255;
        public float G = 255;
        public float R = 255;

        public UnityEngine.Color ToUnityColor()
        {
            return new UnityEngine.Color(R, G, B, A);
        }

        public Color WithAlpha(float alpha)
        {
            return new Color
            {
                R = R, G = G, B = B, A = alpha
            };
        }
    }

    public static class StoryboardModelExtensions
    {
        public static bool IsSet(this int i) => i != int.MinValue;
        public static bool IsSet(this float f) => f != float.MinValue;
        public static bool IsSet(this bool? b) => b != null;
        public static bool IsSet(this string s) => s != null;
        public static bool IsSet(this Color c) => c != null;
        public static bool IsSet<T>(this List<T> l) => l != null;
    }
}