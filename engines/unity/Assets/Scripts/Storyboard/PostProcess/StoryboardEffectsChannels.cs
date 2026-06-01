using UnityEngine;

namespace Cytoid.Storyboard.PostProcess
{
    public sealed class StoryboardEffectsChannels
    {
        public readonly RadialBlurChannel RadialBlur = new();
        public readonly ColorAdjustmentChannel ColorAdjustment = new();
        public readonly FadeChannel GrayScale = new();
        public readonly FadeChannel Sepia = new();
        public readonly NoiseChannel Noise = new();
        public readonly ColorRgbChannel ColorFilter = new();
        public readonly DistortionChannel Dream = new();
        public readonly DistortionChannel Fisheye = new();
        public readonly ShockwaveChannel Shockwave = new();
        public readonly FocusChannel Focus = new();
        public readonly GlitchChannel Glitch = new();
        public readonly ArtifactChannel Artifact = new();
        public readonly ArcadeChannel Arcade = new();
        public readonly ChromaticalChannel Chromatical = new();
        public readonly BoolChannel Tape = new();
        public readonly BloomChannel Bloom = new();

        public void ResetToDefaults()
        {
            RadialBlur.Reset();
            ColorAdjustment.Reset();
            GrayScale.Reset(1f);
            Sepia.Reset(1f);
            Noise.Reset();
            ColorFilter.Reset();
            Dream.Reset(1f);
            Fisheye.Reset(0.5f);
            Shockwave.Reset();
            Focus.Reset();
            Glitch.Reset();
            Artifact.Reset();
            Arcade.Reset();
            Chromatical.Reset();
            Tape.Reset();
            Bloom.Reset();
        }
    }

    public class BoolChannel
    {
        public bool Enabled;

        public void Reset()
        {
            Enabled = false;
        }
    }

    public sealed class FadeChannel : BoolChannel
    {
        public float Fade = 1f;

        public void Reset(float fade = 1f)
        {
            Enabled = false;
            Fade = fade;
        }
    }

    public sealed class RadialBlurChannel : BoolChannel
    {
        public float Intensity = 0.025f;

        public void Reset()
        {
            Enabled = false;
            Intensity = 0.025f;
        }
    }

    public sealed class ColorAdjustmentChannel : BoolChannel
    {
        public float Brightness = 1f;
        public float Saturation = 1f;
        public float Contrast = 1f;

        public void Reset()
        {
            Enabled = false;
            Brightness = 1f;
            Saturation = 1f;
            Contrast = 1f;
        }
    }

    public sealed class NoiseChannel : BoolChannel
    {
        public float Noise = 0.2f;

        public void Reset()
        {
            Enabled = false;
            Noise = 0.2f;
        }
    }

    public sealed class ColorRgbChannel : BoolChannel
    {
        public UnityEngine.Color ColorRgb = UnityEngine.Color.white;

        public void Reset()
        {
            Enabled = false;
            ColorRgb = UnityEngine.Color.white;
        }
    }

    public sealed class DistortionChannel : BoolChannel
    {
        public float Distortion = 1f;

        public void Reset(float distortion = 1f)
        {
            Enabled = false;
            Distortion = distortion;
        }
    }

    public sealed class ShockwaveChannel : BoolChannel
    {
        public float Speed = 1f;
        public float TimeX = 1f;

        public void Reset()
        {
            Enabled = false;
            Speed = 1f;
            TimeX = 1f;
        }
    }

    public sealed class FocusChannel : BoolChannel
    {
        public float Size = 1f;
        public UnityEngine.Color Color = UnityEngine.Color.white;
        public float Speed = 5f;
        public float Intensity = 0.25f;

        public void Reset()
        {
            Enabled = false;
            Size = 1f;
            Color = UnityEngine.Color.white;
            Speed = 5f;
            Intensity = 0.25f;
        }
    }

    public sealed class GlitchChannel : BoolChannel
    {
        public float Glitch = 1f;

        public void Reset()
        {
            Enabled = false;
            Glitch = 1f;
        }
    }

    public sealed class ArtifactChannel : BoolChannel
    {
        public float Fade = 1f;
        public float Colorisation = 1f;
        public float Parasite = 1f;
        public float Noise = 1f;

        public void Reset()
        {
            Enabled = false;
            Fade = 1f;
            Colorisation = 1f;
            Parasite = 1f;
            Noise = 1f;
        }
    }

    public sealed class ArcadeChannel : BoolChannel
    {
        public float InterferanceSize = 1f;
        public float InterferanceSpeed = 0.5f;
        public float Contrast = 1f;
        public float Fade = 1f;

        public void Reset()
        {
            Enabled = false;
            InterferanceSize = 1f;
            InterferanceSpeed = 0.5f;
            Contrast = 1f;
            Fade = 1f;
        }
    }

    public sealed class ChromaticalChannel : BoolChannel
    {
        public float Fade = 1f;
        public float Intensity = 1f;
        public float Speed = 1f;
        public float AnimationTime = 1f;

        public void Reset()
        {
            Enabled = false;
            Fade = 1f;
            Intensity = 1f;
            Speed = 1f;
            AnimationTime = 1f;
        }
    }

    public sealed class BloomChannel : BoolChannel
    {
        public float Intensity;

        public void Reset()
        {
            Enabled = false;
            Intensity = 0f;
        }
    }
}
