using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Newtonsoft.Json;
using SimpleUI;
using UnityEngine;
using UnityEngine.Serialization;

namespace Cytoid.Storyboard
{
    [System.Serializable]
    public class Object<T> where T : ObjectState
    {
        public string Id;
        public List<T> States = new List<T>();

        public bool IsManuallySpawned()
        {
            return States[0].Time == float.MaxValue;
        }
    }

    [System.Serializable]
    public class Text : Object<TextState>
    {
    }

    [System.Serializable]
    public class Sprite : Object<SpriteState>
    {
    }

    [System.Serializable]
    public class Controller : Object<ControllerState>
    {
    }

    [System.Serializable]
    public class ObjectState
    {
        public float
            Time = float
                .MaxValue; // If time is not defined, this object is never rendered - unless cloned and recalculated by a trigger

        public float RelativeTime = float.MinValue;
        public float AddTime = float.MinValue;
        public EasingFunction.Ease Easing;
        public bool Destroy = false;
    }

    [System.Serializable]
    public class SceneObjectState : ObjectState
    {
        public float X = float.MinValue;
        public float Y = float.MinValue;
        public float RotX = float.MinValue;
        public float RotY = float.MinValue;
        public float RotZ = float.MinValue;

        public float ScaleX = float.MinValue;
        public float ScaleY = float.MinValue;

        public float Opacity = float.MinValue;

        public float PivotX = float.MinValue;
        public float PivotY = float.MinValue;

        public float Width = float.MinValue;
        public float Height = float.MinValue;

        public int Layer = int.MinValue;
        public int Order = int.MinValue;
    }

    [System.Serializable]
    public class TextState : SceneObjectState
    {
        public string Font = null;
        public Color Color = null;
        public string Text = null;
        public int Size = int.MinValue;
        public string Align = null;
    }

    [System.Serializable]
    public class SpriteState : SceneObjectState
    {
        public string Path;
        public bool? PreserveAspect = null;
    }

    [System.Serializable]
    public class ControllerState : ObjectState
    {
        public float StoryboardOpacity = float.MinValue;
        public float UiOpacity = float.MinValue;
        public float ScanlineOpacity = float.MinValue;
        public float BackgroundDim = float.MinValue;
        
        public float Size = float.MinValue; // Camera.main.orthographicSize Default: 5
        public float Fov = float.MinValue; // Field of View Default: 53.2
        public bool? Perspective = null;
        public float X = float.MinValue; // Every x/y = 2 * Camera.main.orthographicSize
        public float Y = float.MinValue;
        public float RotX = float.MinValue;
        public float RotY = float.MinValue;
        public float RotZ = float.MinValue;

        public Color ScanlineColor = null;
        public float NoteOpacityMultiplier = float.MinValue;
        public Color NoteRingColor = null;
        public Color[] NoteFillColors = new Color[10];

        public bool? Bloom = null;
        public float BloomIntensity = float.MinValue; // Range: 0~5

        public bool? Vignette = null;
        public float VignetteIntensity = float.MinValue; // Range: 0~1

        public Color VignetteColor = null;
        public float VignetteStart = float.MinValue; // Range: 0~1
        public float VignetteEnd = float.MinValue; // Range: 0~1

        public bool? Chromatic = null;
        public float ChromaticIntensity = float.MinValue; // Range: 0~0.15
        public float ChromaticStart = float.MinValue; // Range: 0~1
        public float ChromaticEnd = float.MinValue; // Range: 0~1

        public bool? RadialBlur = null;
        public float RadialBlurIntensity = float.MinValue; // Range: -0.5~0.5, Default: 0.025

        public bool? ColorAdjustment = null;
        public float Brightness = float.MinValue; // Range: 0~10, Default: 1
        public float Saturation = float.MinValue; // Range: 0~10, Default: 1
        public float Contrast = float.MinValue; // Range: 0~10, Default: 1

        public bool? ColorFilter = null;
        public Color ColorFilterColor = null;

        public bool? GrayScale = null;
        public float GrayScaleIntensity = float.MinValue; // Range: 0~1

        public bool? Noise = null;
        public float NoiseIntensity = float.MinValue; // Range: 0~1, Default: 0.235

        public bool? Sepia = null;
        public float SepiaIntensity = float.MinValue; // Range: 0~1

        public bool? Dream = null;
        public float DreamIntensity = float.MinValue; // Range: 1~10

        public bool? Fisheye = null;
        public float FisheyeIntensity = float.MinValue; // Range: 0~1, Default: 0.5

        public bool? Shockwave = null;
        public float ShockwaveSpeed = float.MinValue; // Range: 0~10, Default: 1

        public bool? Focus = null;
        public float FocusSize = float.MinValue; // Range: 1~10, Default: 1
        public Color FocusColor = null;
        public float FocusSpeed = float.MinValue; // Range: 0~30, Default: 5
        public float FocusIntensity = float.MinValue; // Range: 0~1, Default: 0.25

        public bool? Glitch = null;
        public float GlitchIntensity = float.MinValue; // Range: 0~1

        public bool? Artifact = null;
        public float ArtifactIntensity = float.MinValue; // Range: 0~1
        public float ArtifactColorisation = float.MinValue; // Range: -10~10, Default: 1
        public float ArtifactParasite = float.MinValue; // Range: -10~10, Default: 1
        public float ArtifactNoise = float.MinValue; // Range: -10~10, Default: 1

        public bool? Arcade = null;
        public float ArcadeIntensity = float.MinValue; // Range: 0~1
        public float ArcadeInterferanceSize = float.MinValue; // Range: 0~10, Default: 1
        public float ArcadeInterferanceSpeed = float.MinValue; // Range: 0~10, Default: 0.5
        public float ArcadeContrast = float.MinValue; // Range: 0~10, Default: 1

        public bool? Chromatical = null;
        public float ChromaticalFade = float.MinValue; // Range: 0~1
        public float ChromaticalIntensity = float.MinValue; // Range: 0~1
        public float ChromaticalSpeed = float.MinValue; // Range: 0~3

        public bool? Tape = null;
    }

    [System.Serializable]
    public class Trigger
    {
        public TriggerType Type = TriggerType.None;
        public int Uses = int.MinValue;
        [JsonIgnore] public int CurrentUses;

        public List<int> Notes = new List<int>();
        public List<string> Spawn  = new List<string>();
        public List<string> Destroy  = new List<string>();
        public int Combo = int.MinValue;
        public int Score = int.MinValue;

        [JsonIgnore] public GameNote Triggerer;
    }

    public enum TriggerType
    {
        NoteClear,
        Combo,
        Score,
        None
    }

    [System.Serializable]
    public class Color
    {
        public float R = 255;
        public float G = 255;
        public float B = 255;
        public float A = 1;

        public UnityEngine.Color ToUnityColor()
        {
            return new UnityEngine.Color(R, G, B, A);
        }
    }
}