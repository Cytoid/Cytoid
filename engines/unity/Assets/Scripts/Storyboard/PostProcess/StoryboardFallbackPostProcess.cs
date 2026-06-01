using UnityEngine;

namespace Cytoid.Storyboard.PostProcess
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class StoryboardFallbackPostProcess : MonoBehaviour
    {
        const int BloomSize = 128;

        static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        static readonly int ThresholdId = Shader.PropertyToID("_Threshold");
        static readonly int DirectionId = Shader.PropertyToID("_Direction");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int BloomTexId = Shader.PropertyToID("_BloomTex");
        static readonly int GrayFadeId = Shader.PropertyToID("_GrayFade");
        static readonly int SepiaFadeId = Shader.PropertyToID("_SepiaFade");
        static readonly int ColorRgbId = Shader.PropertyToID("_ColorRgb");
        static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
        static readonly int SaturationId = Shader.PropertyToID("_Saturation");
        static readonly int ContrastId = Shader.PropertyToID("_Contrast");
        static readonly int NoiseAmountId = Shader.PropertyToID("_NoiseAmount");
        static readonly int TimeXId = Shader.PropertyToID("_TimeX");

        Material _colorMaterial;
        Material _brightpassMaterial;
        Material _blurMaterial;
        Material _composeMaterial;

        RenderTexture _bloomDown;
        RenderTexture _bloomBlurH;
        RenderTexture _bloomBlurV;
        RenderTexture _colorTemp;

        float _timeX;

        public StoryboardEffectsChannels Channels { get; private set; }

        public void Bind(StoryboardEffectsChannels channels)
        {
            Channels = channels;
        }

        void OnEnable()
        {
            CreateResources();
        }

        void OnDisable()
        {
            ReleaseResources();
        }

        void CreateResources()
        {
            var colorShader = Shader.Find("Cytoid/Storyboard/Fallback");
            var brightShader = Shader.Find("Cytoid/Storyboard/FallbackBrightpass");
            var blurShader = Shader.Find("Cytoid/Storyboard/FallbackBlur");
            var composeShader = Shader.Find("Cytoid/Storyboard/FallbackBloom");

            if (colorShader == null || brightShader == null || blurShader == null || composeShader == null)
            {
                enabled = false;
                Debug.LogWarning("[StoryboardFallbackPostProcess] Fallback shaders missing; post process disabled.");
                return;
            }

            _colorMaterial = new Material(colorShader) { hideFlags = HideFlags.HideAndDontSave };
            _brightpassMaterial = new Material(brightShader) { hideFlags = HideFlags.HideAndDontSave };
            _blurMaterial = new Material(blurShader) { hideFlags = HideFlags.HideAndDontSave };
            _composeMaterial = new Material(composeShader) { hideFlags = HideFlags.HideAndDontSave };

            _bloomDown = new RenderTexture(BloomSize, BloomSize, 0, RenderTextureFormat.ARGBHalf);
            _bloomBlurH = new RenderTexture(BloomSize, BloomSize, 0, RenderTextureFormat.ARGBHalf);
            _bloomBlurV = new RenderTexture(BloomSize, BloomSize, 0, RenderTextureFormat.ARGBHalf);
            _colorTemp = new RenderTexture(1, 1, 0, RenderTextureFormat.ARGB32);
        }

        void ReleaseResources()
        {
            if (_colorMaterial != null) DestroyImmediate(_colorMaterial);
            if (_brightpassMaterial != null) DestroyImmediate(_brightpassMaterial);
            if (_blurMaterial != null) DestroyImmediate(_blurMaterial);
            if (_composeMaterial != null) DestroyImmediate(_composeMaterial);
            if (_bloomDown != null) _bloomDown.Release();
            if (_bloomBlurH != null) _bloomBlurH.Release();
            if (_bloomBlurV != null) _bloomBlurV.Release();
            if (_colorTemp != null) _colorTemp.Release();
        }

        bool AnyColorPass(StoryboardEffectsChannels c) =>
            c.GrayScale.Enabled || c.Sepia.Enabled || c.ColorFilter.Enabled || c.ColorAdjustment.Enabled ||
            c.Noise.Enabled || UsesUnsupportedAsVignetteHint(c);

        static bool UsesUnsupportedAsVignetteHint(StoryboardEffectsChannels c) =>
            c.Glitch.Enabled || c.Artifact.Enabled || c.Arcade.Enabled || c.Tape.Enabled || c.Chromatical.Enabled ||
            c.Dream.Enabled || c.Fisheye.Enabled || c.Shockwave.Enabled || c.Focus.Enabled || c.RadialBlur.Enabled;

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (Channels == null || _colorMaterial == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            var channels = Channels;
            _timeX += Time.deltaTime;

            var current = source;
            RenderTexture colorProcessed = null;

            if (AnyColorPass(channels))
            {
                EnsureColorTemp(source);
                ApplyColorKeywords(channels);
                Graphics.Blit(current, _colorTemp, _colorMaterial);
                current = _colorTemp;
                colorProcessed = _colorTemp;
            }

            if (channels.Bloom.Enabled && channels.Bloom.Intensity > 0f)
            {
                _brightpassMaterial.SetFloat(ThresholdId, 0.7f);
                Graphics.Blit(current, _bloomDown, _brightpassMaterial);

                _blurMaterial.SetVector(DirectionId, new Vector4(1, 0, 0, 0));
                Graphics.Blit(_bloomDown, _bloomBlurH, _blurMaterial);
                _blurMaterial.SetVector(DirectionId, new Vector4(0, 1, 0, 0));
                Graphics.Blit(_bloomBlurH, _bloomBlurV, _blurMaterial);

                _composeMaterial.SetTexture(BloomTexId, _bloomBlurV);
                _composeMaterial.SetFloat(IntensityId, channels.Bloom.Intensity);
                Graphics.Blit(current, destination, _composeMaterial);
            }
            else if (colorProcessed != null)
            {
                Graphics.Blit(colorProcessed, destination);
            }
            else
            {
                Graphics.Blit(source, destination);
            }
        }

        void EnsureColorTemp(RenderTexture source)
        {
            if (_colorTemp.width == source.width && _colorTemp.height == source.height) return;
            _colorTemp.Release();
            _colorTemp.width = source.width;
            _colorTemp.height = source.height;
            _colorTemp.Create();
        }

        void ApplyColorKeywords(StoryboardEffectsChannels c)
        {
            SetKeyword(_colorMaterial, "GRAYSCALE_ON", c.GrayScale.Enabled);
            SetKeyword(_colorMaterial, "SEPIA_ON", c.Sepia.Enabled);
            SetKeyword(_colorMaterial, "COLOR_FILTER_ON", c.ColorFilter.Enabled);
            SetKeyword(_colorMaterial, "COLOR_ADJUST_ON", c.ColorAdjustment.Enabled);
            SetKeyword(_colorMaterial, "NOISE_ON", c.Noise.Enabled);
            SetKeyword(_colorMaterial, "VIGNETTE_HINT_ON", UsesUnsupportedAsVignetteHint(c));

            _colorMaterial.SetFloat(GrayFadeId, c.GrayScale.Fade);
            _colorMaterial.SetFloat(SepiaFadeId, c.Sepia.Fade);
            _colorMaterial.SetColor(ColorRgbId, c.ColorFilter.ColorRgb);
            _colorMaterial.SetFloat(BrightnessId, c.ColorAdjustment.Brightness);
            _colorMaterial.SetFloat(SaturationId, c.ColorAdjustment.Saturation);
            _colorMaterial.SetFloat(ContrastId, c.ColorAdjustment.Contrast);
            _colorMaterial.SetFloat(NoiseAmountId, c.Noise.Noise);
            _colorMaterial.SetFloat(TimeXId, _timeX);
        }

        static void SetKeyword(Material material, string keyword, bool enabled)
        {
            if (enabled) material.EnableKeyword(keyword);
            else material.DisableKeyword(keyword);
        }
    }
}
