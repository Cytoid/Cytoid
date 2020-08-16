using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Cytoid.Storyboard
{

    [Serializable]
    public abstract class Object
    {
        public string Id;
        public string TargetId;
        public string ParentId;

        public abstract bool IsManuallySpawned();

        public abstract void FindStates(float time, out ObjectState currentState, out ObjectState nextState);
    }
    
    [Serializable]
    public class Object<T> : Object where T : ObjectState
    {
        public List<T> States = new List<T>();

        public override bool IsManuallySpawned()
        {
            return States[0].Time == float.MaxValue;
        }
        
        public override void FindStates(float time, out ObjectState currentState, out ObjectState nextState)
        {
            // TODO: Offline lookup generation?
            
            if (States.Count == 0)
            {
                currentState = null;
                nextState = null;
                return;
            }

            for (var i = 0; i < States.Count; i++)
                if (States[i].Time > time) // Next state
                {
                    // Current state is the previous state
                    currentState = i > 0 ? States[i - 1] : null;
                    nextState = States[i];
                    return;
                }

            currentState = nextState = States.Last();
        }
    }

    [Serializable]
    public class StageObject<TS> : Object<TS> where TS : StageObjectState
    {
        public string TargetId;
    }

    [Serializable]
    public class Text : StageObject<TextState>
    {
    }

    [Serializable]
    public class Sprite : StageObject<SpriteState>
    {
    }

    [Serializable]
    public class Video : StageObject<VideoState>
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
    public class Line : StageObject<LineState>
    {
    }

    [Serializable]
    public class ObjectState
    {
        public float? AddTime;
        public bool? Destroy;
        public EasingFunction.Ease? Easing;

        public float? RelativeTime;

        // If time is not defined, this object is never rendered (hence float.MaxValue) - unless cloned and recalculated by a trigger
        public float Time = float.MaxValue;
    }

    [Serializable]
    public class StageObjectState : ObjectState
    {
        public bool? FillWidth;
        public UnitFloat Height;

        public int? Layer;

        public float? Opacity;
        public int? Order;

        public float? PivotX;
        public float? PivotY ;
        public float? RotX ;
        public float? RotY ;
        public float? RotZ ;

        public float? ScaleX ;
        public float? ScaleY ;

        public UnitFloat Width ;
        public UnitFloat X ;
        public UnitFloat Y ;
        public UnitFloat Z ;
    }

    [Serializable]
    public class TextState : StageObjectState
    {
        public string Align;
        public Color Color;
        public string Font;
        public int? Size;
        public string Text;
        public float? LetterSpacing;
        public FontWeight? FontWeight;
    }

    [Serializable]
    public class SpriteState : StageObjectState
    {
        public Color Color;
        public string Path;
        public bool? PreserveAspect;
    }

    [Serializable]
    public class VideoState : StageObjectState
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
        public int? Direction;
        public float MinX = int.MinValue;
        public float MaxX = int.MaxValue;
    }

    [Serializable]
    public class NoteControllerState : ObjectState
    {
        public int? Note;
        public bool? OverrideX;
        public UnitFloat X;
        public float? XMultiplier;
        public float? XOffset;
        public bool? OverrideY;
        public UnitFloat Y;
        public float? YMultiplier;
        public float? YOffset;
        public bool? OverrideZ;
        public UnitFloat Z;
        public bool? OverrideRotX;
        public float? RotX;
        public bool? OverrideRotY;
        public float? RotY;
        public bool? OverrideRotZ;
        public float? RotZ;
        public bool? OverrideRingColor;
        public Color RingColor;
        public bool? OverrideFillColor;
        public Color FillColor;
        public float? OpacityMultiplier;
        public float? SizeMultiplier;
        public int? HoldDirection;
        public int? Style;
    }

    [Serializable]
    public class LinePosition
    {
        public UnitFloat X;
        public UnitFloat Y;
        public UnitFloat Z;
    }

    [Serializable]
    public class LineState : StageObjectState
    {
        public List<LinePosition> Pos = new List<LinePosition>();
        public UnitFloat Width;
        public Color Color;
        public float? Opacity;
        public int? Layer;
        public int? Order;
    }

    [Serializable]
    public class ControllerState : ObjectState
    {
        public bool? Arcade;
        public float? ArcadeContrast; // Range: 0~10, Default: 1
        public float? ArcadeIntensity; // Range: 0~1
        public float? ArcadeInterferanceSize; // Range: 0~10, Default: 1
        public float? ArcadeInterferanceSpeed; // Range: 0~10, Default: 0.5

        public bool? Artifact;
        public float? ArtifactColorisation; // Range: -10~10, Default: 1
        public float? ArtifactIntensity; // Range: 0~1
        public float? ArtifactNoise; // Range: -10~10, Default: 1
        public float? ArtifactParasite; // Range: -10~10, Default: 1
        public float? BackgroundDim;

        public bool? Bloom;
        public float? BloomIntensity; // Range: 0~5
        public float? Brightness; // Range: 0~10, Default: 1

        public bool? Chromatic;

        public bool? Chromatical;
        public float? ChromaticalFade; // Range: 0~1
        public float? ChromaticalIntensity; // Range: 0~1
        public float? ChromaticalSpeed; // Range: 0~3
        public float? ChromaticEnd; // Range: 0~1
        public float? ChromaticIntensity; // Range: 0~0.15
        public float? ChromaticStart; // Range: 0~1

        public bool? ColorAdjustment;

        public bool? ColorFilter;
        public Color ColorFilterColor;
        public float? Contrast; // Range: 0~10, Default: 1

        public bool? Dream;
        public float? DreamIntensity; // Range: 1~10

        public bool? Fisheye;
        public float? FisheyeIntensity; // Range: 0~1, Default: 0.5

        public bool? Focus;
        public Color FocusColor;
        public float? FocusIntensity; // Range: 0~1, Default: 0.25
        public float? FocusSize; // Range: 1~10, Default: 1
        public float? FocusSpeed; // Range: 0~30, Default: 5
        public float? Fov; // Field of View Default: 53.2

        public bool? Glitch;
        public float? GlitchIntensity; // Range: 0~1

        public bool? GrayScale;
        public float? GrayScaleIntensity; // Range: 0~1

        public bool? Noise;
        public float? NoiseIntensity; // Range: 0~1, Default: 0.235
        public List<Color> NoteFillColors;
        public float? NoteOpacityMultiplier;
        public Color NoteRingColor;
        public bool? OverrideScanlinePos;
        public bool? Perspective;

        public bool? RadialBlur;
        public float? RadialBlurIntensity; // Range: -0.5~0.5, Default: 0.025
        public float? RotX;
        public float? RotY;
        public float? RotZ;
        public float? Saturation; // Range: 0~10, Default: 1

        public Color ScanlineColor;
        public float? ScanlineOpacity;
        public UnitFloat ScanlinePos;
        public bool? ScanlineSmoothing;

        public bool? Sepia;
        public float? SepiaIntensity; // Range: 0~1

        public bool? Shockwave;
        public float? ShockwaveSpeed; // Range: 0~10, Default: 1

        public float? Size; // Camera.main.orthographicSize Default: 5
        public float? StoryboardOpacity;

        public bool? Tape;
        public float? UiOpacity;

        public bool? Vignette;

        public Color VignetteColor;
        public float? VignetteEnd; // Range: 0~1
        public float? VignetteIntensity; // Range: 0~1
        public float? VignetteStart; // Range: 0~1
        
        public UnitFloat X; // Every x/y = 2 * Camera.main.orthographicSize
        public UnitFloat Y;
        public UnitFloat Z;
    }

    [Serializable]
    public class Trigger
    {
        public int? Combo;
        [JsonIgnore] public int CurrentUses;
        public List<string> Destroy = new List<string>();

        public List<int> Notes = new List<int>();
        public int? Score;
        public List<string> Spawn = new List<string>();

        [JsonIgnore] public Note Triggerer;
        public TriggerType Type = TriggerType.None;
        public int? Uses;
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

}