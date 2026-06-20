using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Cytoid.Storyboard.Controllers
{
    public class ControllerStateParser : GenericStateParser<ControllerState>
    {
        public ControllerStateParser(Storyboard storyboard) : base(storyboard)
        {
        }

        public override void Parse(ControllerState state, JObject json, ControllerState baseState)
        {
            ParseObjectState(state, json, baseState);

            state.StoryboardOpacity = (float?) json.SelectToken("storyboard_opacity") ?? state.StoryboardOpacity;
            state.UiOpacity = (float?) json.SelectToken("ui_opacity") ?? state.UiOpacity;
            state.ScanlineOpacity = (float?) json.SelectToken("scanline_opacity") ?? state.ScanlineOpacity;
            state.BackgroundDim = (float?) json.SelectToken("background_dim") ?? state.BackgroundDim;

            state.Size = (float?) json.SelectToken("size") ?? state.Size;
            state.Fov = (float?) json.SelectToken("fov") ?? state.Fov;
            state.Perspective = (bool?) json.SelectToken("perspective") ?? state.Perspective;
            state.X = ParseUnitFloat(json.SelectToken("x"), ReferenceUnit.CameraX, false, false) ?? state.X;
            state.Y = ParseUnitFloat(json.SelectToken("y"), ReferenceUnit.CameraY, false, false) ?? state.Y;
            state.Z = ParseUnitFloat(json.SelectToken("z"), ReferenceUnit.World, false, false) ?? state.Z;
            state.RotX = (float?) json.SelectToken("rot_x") ?? state.RotX;
            state.RotY = (float?) json.SelectToken("rot_y") ?? state.RotY;
            state.RotZ = (float?) json.SelectToken("rot_z") ?? state.RotZ;

            if (ColorUtility.TryParseHtmlString((string) json.SelectToken("scanline_color"), out var tmp))
                state.ScanlineColor = new Color {R = tmp.r, G = tmp.g, B = tmp.b, A = tmp.a};
            state.ScanlineSmoothing = (bool?) json.SelectToken("scanline_smoothing") ?? state.ScanlineSmoothing;
            state.OverrideScanlinePos = (bool?) json.SelectToken("override_scanline_pos") ?? state.OverrideScanlinePos;
            state.ScanlinePos = ParseUnitFloat(json.SelectToken("scanline_pos"), ReferenceUnit.NoteY, false, false) ?? state.ScanlinePos;
            state.NoteOpacityMultiplier =
                (float?) json.SelectToken("note_opacity_multiplier") ?? state.NoteOpacityMultiplier;
            if (ColorUtility.TryParseHtmlString((string) json.SelectToken("note_ring_color"), out tmp))
                state.NoteRingColor = new Color {R = tmp.r, G = tmp.g, B = tmp.b, A = tmp.a};
            var fillColors = json.SelectToken("note_fill_colors") != null &&
                             json.SelectToken("note_fill_colors").Type != JTokenType.Null
                ? json.SelectToken("note_fill_colors").Values<string>().ToList()
                : new List<string>();
            if (fillColors.Count > 0)
            {
                state.NoteFillColors = new List<Color>();
                for (var i = 0; i < 12; i++)
                {
                    if (i >= fillColors.Count)
                    {
                        state.NoteFillColors.Add(new Color {R=0, G=0, B=0, A=0});
                        continue;
                    }

                    var fillColor = fillColors[i];
                    if (ColorUtility.TryParseHtmlString(fillColor, out tmp))
                        state.NoteFillColors.Add(new Color {R = tmp.r, G = tmp.g, B = tmp.b, A = tmp.a});
                }
            }

            state.Bloom = (bool?) json.SelectToken("bloom") ?? state.Bloom;
            state.BloomIntensity = (float?) json.SelectToken("bloom_intensity") ?? state.BloomIntensity;

            state.Vignette = (bool?) json.SelectToken("vignette") ?? state.Vignette;
            state.VignetteIntensity = (float?) json.SelectToken("vignette_intensity") ?? state.VignetteIntensity;

            if (ColorUtility.TryParseHtmlString((string) json.SelectToken("vignette_color"), out tmp))
                state.VignetteColor = new Color {R = tmp.r, G = tmp.g, B = tmp.b, A = tmp.a};
            state.VignetteStart = (float?) json.SelectToken("vignette_start") ?? state.VignetteStart;
            state.VignetteEnd = (float?) json.SelectToken("vignette_end") ?? state.VignetteEnd;

            state.Chromatic = (bool?) json.SelectToken("chromatic") ?? state.Chromatic;
            state.ChromaticIntensity = (float?) json.SelectToken("chromatic_intensity") ?? state.ChromaticIntensity;
            state.ChromaticStart = (float?) json.SelectToken("chromatic_start") ?? state.ChromaticStart;
            state.ChromaticEnd = (float?) json.SelectToken("chromatic_end") ?? state.ChromaticEnd;

            state.RadialBlur = (bool?) json.SelectToken("radial_blur") ?? state.RadialBlur;
            state.RadialBlurIntensity = (float?) json.SelectToken("radial_blur_intensity") ?? state.RadialBlurIntensity;

            state.ColorAdjustment = (bool?) json.SelectToken("color_adjustment") ?? state.ColorAdjustment;
            state.Brightness = (float?) json.SelectToken("brightness") ?? state.Brightness;
            state.Saturation = (float?) json.SelectToken("saturation") ?? state.Saturation;
            state.Contrast = (float?) json.SelectToken("contrast") ?? state.Contrast;

            state.ColorFilter = (bool?) json.SelectToken("color_filter") ?? state.ColorFilter;
            if (ColorUtility.TryParseHtmlString((string) json.SelectToken("color_filter_color"), out tmp))
                state.ColorFilterColor = new Color {R = tmp.r, G = tmp.g, B = tmp.b, A = tmp.a};

            state.GrayScale = (bool?) json.SelectToken("gray_scale") ?? state.GrayScale;
            state.GrayScaleIntensity = (float?) json.SelectToken("gray_scale_intensity") ?? state.GrayScaleIntensity;

            state.Noise = (bool?) json.SelectToken("noise") ?? state.Noise;
            state.NoiseIntensity = (float?) json.SelectToken("noise_intensity") ?? state.NoiseIntensity;

            state.Sepia = (bool?) json.SelectToken("sepia") ?? state.Sepia;
            state.SepiaIntensity = (float?) json.SelectToken("sepia_intensity") ?? state.SepiaIntensity;

            state.Dream = (bool?) json.SelectToken("dream") ?? state.Dream;
            state.DreamIntensity = (float?) json.SelectToken("dream_intensity") ?? state.DreamIntensity;

            state.Fisheye = (bool?) json.SelectToken("fisheye") ?? state.Fisheye;
            state.FisheyeIntensity = (float?) json.SelectToken("fisheye_intensity") ?? state.FisheyeIntensity;

            state.Shockwave = (bool?) json.SelectToken("shockwave") ?? state.Shockwave;
            state.ShockwaveSpeed = (float?) json.SelectToken("shockwave_speed") ?? state.ShockwaveSpeed;

            state.Focus = (bool?) json.SelectToken("focus") ?? state.Focus;
            state.FocusIntensity = (float?) json.SelectToken("focus_intensity") ?? state.FocusIntensity;
            state.FocusSize = (float?) json.SelectToken("focus_size") ?? state.FocusSize;
            state.FocusSpeed = (float?) json.SelectToken("focus_speed") ?? state.FocusSpeed;
            if (ColorUtility.TryParseHtmlString((string) json.SelectToken("focus_color"), out tmp))
                state.FocusColor = new Color {R = tmp.r, G = tmp.g, B = tmp.b, A = tmp.a};

            state.Glitch = (bool?) json.SelectToken("glitch") ?? state.Glitch;
            state.GlitchIntensity = (float?) json.SelectToken("glitch_intensity") ?? state.GlitchIntensity;

            state.Artifact = (bool?) json.SelectToken("artifact") ?? state.Artifact;
            state.ArtifactIntensity = (float?) json.SelectToken("artifact_intensity") ?? state.ArtifactIntensity;
            state.ArtifactColorisation =
                (float?) json.SelectToken("artifact_colorisation") ?? state.ArtifactColorisation;
            state.ArtifactParasite = (float?) json.SelectToken("artifact_parasite") ?? state.ArtifactParasite;
            state.ArtifactNoise = (float?) json.SelectToken("artifact_noise") ?? state.ArtifactNoise;

            state.Arcade = (bool?) json.SelectToken("arcade") ?? state.Arcade;
            state.ArcadeIntensity = (float?) json.SelectToken("arcade_intensity") ?? state.ArcadeIntensity;
            state.ArcadeInterferanceSize =
                (float?) json.SelectToken("arcade_interference_size") ?? state.ArcadeInterferanceSize;
            state.ArcadeInterferanceSpeed =
                (float?) json.SelectToken("arcade_interference_speed") ?? state.ArcadeInterferanceSpeed;
            state.ArcadeContrast = (float?) json.SelectToken("arcade_contrast") ?? state.ArcadeContrast;

            state.Chromatical = (bool?) json.SelectToken("chromatical") ?? state.Chromatical;
            state.ChromaticalFade = (float?) json.SelectToken("chromatical_fade") ?? state.ChromaticalFade;
            state.ChromaticalIntensity =
                (float?) json.SelectToken("chromatical_intensity") ?? state.ChromaticalIntensity;
            state.ChromaticalSpeed = (float?) json.SelectToken("chromatical_speed") ?? state.ChromaticalSpeed;

            state.Tape = (bool?) json.SelectToken("tape") ?? state.Tape;
        }
    }
}