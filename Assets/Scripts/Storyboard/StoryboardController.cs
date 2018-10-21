using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cytus2.Controllers;
using Cytus2.Models;
using Newtonsoft.Json;
using QuickEngine.Extensions;
using SleekRender;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cytoid.Storyboard
{
    public class StoryboardController : SingletonMonoBehavior<StoryboardController>
    {
        public bool ShowEffects = true;

        private string lastPath;
        private FileSystemWatcher watcher;

        public UnityEngine.UI.Text TextPrefab;
        public Image SpritePrefab;
        public PrismEffects Prism;
        public CameraFilterPack_Blur_Radial_Fast RadialBlur;
        public CameraFilterPack_Color_BrightContrastSaturation ColorAdjustment;
        public CameraFilterPack_Color_GrayScale GrayScale;
        public CameraFilterPack_Color_Noise Noise;
        public CameraFilterPack_Color_RGB ColorFilter;
        public CameraFilterPack_Color_Sepia Sepia;
        public CameraFilterPack_Distortion_Dream Dream;
        public CameraFilterPack_Distortion_FishEye Fisheye;
        public CameraFilterPack_Distortion_ShockWave Shockwave;
        public CameraFilterPack_Drawing_Manga_Flash_Color Focus;
        public CameraFilterPack_FX_Glitch1 Glitch;
        public CameraFilterPack_TV_Artefact Artifact;
        public CameraFilterPack_TV_ARCADE_2 Arcade;
        public CameraFilterPack_TV_Chromatical Chromatical;
        public CameraFilterPack_TV_Videoflip Tape;
        public SleekRenderPostProcess Sleek;
        public Canvas Canvas;
        public CanvasGroup UiCanvasGroup;
        public Image BackgroundOverlayMask;

        [HideInInspector] public CanvasGroup CanvasGroup;
        [HideInInspector] public Rect CanvasRect;

        [HideInInspector] public Storyboard Storyboard;

        [HideInInspector]
        public Dictionary<UnityEngine.UI.Text, Text> TextViews = new Dictionary<UnityEngine.UI.Text, Text>();

        [HideInInspector] public Dictionary<Image, Sprite> SpriteViews = new Dictionary<Image, Sprite>();
        [HideInInspector] public List<Controller> Controllers = new List<Controller>();
        [HideInInspector] public List<Trigger> Triggers = new List<Trigger>();

        public static float Time
        {
            get { return Game.Instance.Time; }
        }

        public void CompileStoryboard()
        {
            var level = Game.Instance.Level;
            var path = level.BasePath + "/storyboard_compiled.json";
            File.WriteAllText(path, JsonConvert.SerializeObject(Storyboard.Compile()));
        }

        public void Reset()
        {
            foreach (var text in TextViews.Keys)
            {
                text.fontSize = 20;
                text.alignment = TextAnchor.MiddleCenter;
                text.text = "こんにちは";
                text.color = UnityEngine.Color.white;
                text.GetComponent<CanvasGroup>().alpha = 0;
            }

            foreach (var sprite in SpriteViews.Keys)
            {
                sprite.color = UnityEngine.Color.white;
                sprite.preserveAspect = true;
                sprite.GetComponent<CanvasGroup>().alpha = 0;
            }

            Camera.main.transform.position = new Vector3(0, 0, -10);
            Camera.main.transform.eulerAngles = Vector3.zero;
            Camera.main.orthographic = true;
            Camera.main.fieldOfView = 53.2f;

            Prism.SetPrismPreset(null);
            Prism.useVignette = false;
            Prism.useChromaticAberration = false;
            Sleek.settings.bloomEnabled = false;
            Sleek.settings.bloomIntensity = 0;

            RadialBlur.enabled = false;
            RadialBlur.Intensity = 0.025f;
            ColorAdjustment.enabled = false;
            ColorAdjustment.Brightness = 1;
            ColorAdjustment.Saturation = 1;
            ColorAdjustment.Contrast = 1;
            GrayScale.enabled = false;
            GrayScale._Fade = 1;
            Noise.enabled = false;
            Noise.Noise = 0.2f;
            ColorFilter.enabled = false;
            ColorFilter.ColorRGB = UnityEngine.Color.white;
            Sepia.enabled = false;
            Sepia._Fade = 1;
            Dream.enabled = false;
            Dream.Distortion = 1f;
            Fisheye.enabled = false;
            Fisheye.Distortion = 0.5f;
            Shockwave.enabled = false;
            Shockwave.TimeX = 1.0f;
            Shockwave.Speed = 1;
            Focus.enabled = false;
            Focus.Size = 1;
            Focus.Color = UnityEngine.Color.white;
            Focus.Speed = 5;
            Focus.Intensity = 0.25f;
            Glitch.enabled = false;
            Glitch.Glitch = 1;
            Artifact.enabled = false;
            Artifact.Fade = 1;
            Artifact.Colorisation = 1;
            Artifact.Parasite = 1;
            Artifact.Noise = 1;
            Arcade.enabled = false;
            Arcade.Interferance_Size = 1;
            Arcade.Interferance_Speed = 0.5f;
            Arcade.Contrast = 1;
            Arcade.Fade = 1;
            Chromatical.enabled = false;
            Chromatical.Fade = 1;
            Chromatical.Intensity = 1;
            Chromatical.Speed = 1;
            Tape.enabled = false;
        }

        private new void Awake()
        {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            Environment.SetEnvironmentVariable("MONO_MANAGED_WATCHER", "enabled");
#endif
        }

        private IEnumerator Start()
        {
            // Don't proceed until game chart is ready
            while (Game.Instance == null || Game.Instance.Chart == null)
            {
                yield return null;
            }

            CanvasGroup = Canvas.GetComponent<CanvasGroup>();
            CanvasRect = Canvas.GetComponent<RectTransform>().rect;

            var level = Game.Instance.Level;
            var chartSection = level.charts.Find(it => it.type == CytoidApplication.CurrentChartType);

            var path = level.BasePath + "/" +
                       (chartSection.storyboard != null ? chartSection.storyboard.path : "storyboard.json");

            if (!File.Exists(path))
            {
                enabled = false;
                yield break;
            }

            if (SceneManager.GetActiveScene().name == "Storyboard")
            {
                WatchFileUpdate();
            }

            // Listen to events
            EventKit.Subscribe<GameNote>("note clear", OnNoteClear);

            yield return Reload(path);

            SetupGraphics();
        }

        public void SetupGraphics()
        {
            if (Controllers.Count > 0)
            {
                switch (PlayerPrefs.GetString("storyboard effects"))
                {
                    case "High":
                        Screen.SetResolution(CytoidApplication.OriginalWidth, CytoidApplication.OriginalHeight, true);
                        break;
                    case "Medium":
                        Screen.SetResolution((int) (CytoidApplication.OriginalWidth * 0.75),
                            (int) (CytoidApplication.OriginalHeight * 0.75), true);
                        break;
                    case "Low":
                        Screen.SetResolution((int) (CytoidApplication.OriginalWidth * 0.5),
                            (int) (CytoidApplication.OriginalHeight * 0.5), true);
                        break;
                    case "None":
                        ShowEffects = false;
                        break;
                }
            }

            if (PlayerPrefsExt.GetBool("low res"))
            {
                Screen.SetResolution((int) (CytoidApplication.OriginalWidth * 0.5),
                    (int) (CytoidApplication.OriginalHeight * 0.5), true);
            }

            // Enable PRISM only if supported
            if (Prism.m_Shader.isSupported && Prism.m_Shader2.isSupported && Prism.m_Shader3.isSupported)
            {
                Prism.enabled = true;
                Prism.EnableEffects();
            }
        }

        public void WatchFileUpdate()
        {
            var path = Application.persistentDataPath + "/player/storyboard.json";
            // Watch for file changes
            watcher = new FileSystemWatcher
            {
                Filter = Path.GetFileName(path),
                Path = Path.GetDirectoryName(path),
                NotifyFilter = NotifyFilters.LastWrite
            };
            watcher.Changed += delegate
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() => { StartCoroutine(Reload(path)); });
            };
            watcher.EnableRaisingEvents = true;
        }

        public IEnumerator Reload(string path = null)
        {
            if (path == null) path = lastPath;
            lastPath = path;

            // Clear texts
            foreach (var textView in TextViews.Keys)
            {
                Destroy(textView.gameObject);
            }

            TextViews.Clear();

            // Clear sprites
            foreach (var spriteView in SpriteViews.Keys)
            {
                Destroy(spriteView.gameObject);
            }

            SpriteViews.Clear();

            // Clear scene
            Controllers.Clear();

            // Clear triggers
            Triggers.Clear();

            // Reload storyboard
            Storyboard = new Storyboard(File.ReadAllText(path));

            // Create texts
            foreach (var text in Storyboard.Texts)
            {
                if (!text.IsManuallySpawned()) Spawn(text);
            }

            // Create sprites
            foreach (var sprite in Storyboard.Sprites)
            {
                if (!sprite.IsManuallySpawned()) yield return Spawn(sprite);
            }

            // Create scene
            Controllers = Storyboard.Controllers;

            // Initialize triggers
            Triggers = Storyboard.Triggers;

            Resources.UnloadUnusedAssets();
        }

        private void Update()
        {
            if (Time < 0) return;
            if (!Game.Instance.IsPlaying)
            {
                Reset();
                return;
            }

            UpdateTexts();

            UpdateSprites();

            UpdateControllers();

            if (Testing)
            {
                UpdateTestViews();
            }
        }

        protected virtual void UpdateTexts()
        {
            foreach (var textView in TextViews.Keys.ToList())
            {
                var text = TextViews[textView];

                TextState a;
                TextState b;
                FindStates(text.States, out a, out b);

                if (a == null)
                {
                    continue;
                }

                stateA = a;
                stateB = b;
                ease = a.Easing;

                // Destroy
                if (a.Destroy)
                {
                    Destroy(textView.gameObject);
                    TextViews.Remove(textView);
                    continue;
                }

                // X
                if (a.X != float.MinValue)
                {
                    textView.rectTransform.SetLocalX(EaseCanvasX(a.X, b.X));
                }

                // Y
                if (a.Y != float.MinValue)
                {
                    textView.rectTransform.SetLocalY(EaseCanvasY(a.Y, b.Y));
                }

                // RotX
                if (a.RotX != float.MinValue)
                {
                    textView.rectTransform.localEulerAngles =
                        textView.rectTransform.localEulerAngles.SetX(Ease(a.RotX, b.RotX));
                }

                // RotY
                if (a.RotY != float.MinValue)
                {
                    textView.rectTransform.localEulerAngles =
                        textView.rectTransform.localEulerAngles.SetY(Ease(a.RotY, b.RotY));
                }

                // RotZ
                if (a.RotZ != float.MinValue)
                {
                    textView.rectTransform.localEulerAngles =
                        textView.rectTransform.localEulerAngles.SetZ(Ease(a.RotZ, b.RotZ));
                }

                // ScaleX
                if (a.ScaleX != float.MinValue)
                {
                    textView.rectTransform.SetScaleX(Ease(a.ScaleX, b.ScaleX));
                }

                // ScaleY
                if (a.ScaleY != float.MinValue)
                {
                    textView.rectTransform.SetScaleY(Ease(a.ScaleY, b.ScaleY));
                }

                // Color
                if (a.Color != null)
                {
                    textView.color = b.Color == null
                        ? a.Color.ToUnityColor()
                        : UnityEngine.Color.Lerp(a.Color.ToUnityColor(), b.Color.ToUnityColor(), Ease(0, 1));
                }

                // Opacity
                if (a.Opacity != float.MinValue)
                {
                    textView.GetComponent<CanvasGroup>().alpha = Ease(a.Opacity, b.Opacity);
                }

                // PivotX
                if (a.PivotX != float.MinValue)
                {
                    textView.rectTransform.pivot =
                        new Vector2(Ease(a.PivotX, b.PivotX), textView.rectTransform.pivot.y);
                }

                // PivotY
                if (a.PivotY != float.MinValue)
                {
                    textView.rectTransform.pivot =
                        new Vector2(textView.rectTransform.pivot.x, Ease(a.PivotY, b.PivotY));
                }

                // Width
                if (a.Width != float.MinValue)
                {
                    textView.rectTransform.SetWidth(EaseCanvasX(a.Width, b.Width));
                }

                // Height
                if (a.Height != float.MinValue)
                {
                    textView.rectTransform.SetHeight(EaseCanvasY(a.Height, b.Height));
                }

                // Text
                if (a.Text != null)
                {
                    textView.text = a.Text;
                }

                // Size
                if (a.Size != int.MinValue)
                {
                    textView.fontSize = a.Size;
                }

                // Align
                if (a.Align != null)
                {
                    textView.alignment =
                        (TextAnchor) Enum.Parse(typeof(TextAnchor), a.Align, true);
                }

                Canvas canvas = null;

                // Layer
                if (a.Layer != int.MinValue)
                {
                    a.Layer = Mathf.Clamp(a.Layer, 0, 2);
                    canvas = textView.GetComponent<Canvas>();
                    canvas.overrideSorting = true;
                    canvas.sortingLayerName = "Storyboard" + (a.Layer + 1);
                }

                // Order
                if (a.Order != int.MinValue)
                {
                    if (canvas == null) canvas = textView.GetComponent<Canvas>();
                    canvas.sortingOrder = a.Order;
                }
            }
        }

        protected virtual void UpdateSprites()
        {
            foreach (var spriteView in SpriteViews.Keys.ToList())
            {
                var sprite = SpriteViews[spriteView];

                SpriteState a;
                SpriteState b;
                FindStates(sprite.States, out a, out b);

                if (a == null)
                {
                    continue;
                }

                stateA = a;
                stateB = b;
                ease = a.Easing;

                // Destroy
                if (a.Destroy)
                {
                    Destroy(spriteView.gameObject);
                    SpriteViews.Remove(spriteView);
                    continue;
                }

                // X
                if (a.X != float.MinValue)
                {
                    spriteView.rectTransform.SetLocalX(EaseCanvasX(a.X, b.X));
                }

                // Y
                if (a.Y != float.MinValue)
                {
                    spriteView.rectTransform.SetLocalY(EaseCanvasY(a.Y, b.Y));
                }

                // RotX
                if (a.RotX != float.MinValue)
                {
                    spriteView.rectTransform.localEulerAngles =
                        spriteView.rectTransform.localEulerAngles.SetX(Ease(a.RotX, b.RotX));
                }

                // RotY
                if (a.RotY != float.MinValue)
                {
                    spriteView.rectTransform.localEulerAngles =
                        spriteView.rectTransform.localEulerAngles.SetY(Ease(a.RotY, b.RotY));
                }

                // RotZ
                if (a.RotZ != float.MinValue)
                {
                    spriteView.rectTransform.localEulerAngles =
                        spriteView.rectTransform.localEulerAngles.SetZ(Ease(a.RotZ, b.RotZ));
                }

                // ScaleX
                if (a.ScaleX != float.MinValue)
                {
                    spriteView.rectTransform.SetScaleX(Ease(a.ScaleX, b.ScaleX));
                }

                // ScaleY
                if (a.ScaleY != float.MinValue)
                {
                    spriteView.rectTransform.SetScaleY(Ease(a.ScaleY, b.ScaleY));
                }

                // Opacity
                if (a.Opacity != float.MinValue)
                {
                    spriteView.GetComponent<CanvasGroup>().alpha = Ease(a.Opacity, b.Opacity);
                }

                // PivotX
                if (a.PivotX != float.MinValue)
                {
                    spriteView.rectTransform.pivot =
                        new Vector2(Ease(a.PivotX, b.PivotX), spriteView.rectTransform.pivot.y);
                }

                // PivotY
                if (a.PivotY != float.MinValue)
                {
                    spriteView.rectTransform.pivot =
                        new Vector2(spriteView.rectTransform.pivot.x, Ease(a.PivotY, b.PivotY));
                }

                // Width
                if (a.Width != float.MinValue)
                {
                    spriteView.rectTransform.SetWidth(EaseCanvasX(a.Width, b.Width));
                }

                // Height
                if (a.Height != float.MinValue)
                {
                    spriteView.rectTransform.SetHeight(EaseCanvasY(a.Height, b.Height));
                }

                // Preserve aspect
                if (a.PreserveAspect != null)
                {
                    spriteView.preserveAspect = (bool) a.PreserveAspect;
                }
                
                // Color tint
                if (a.Color != null)
                {
                    var easedColor = b.Color == null
                        ? a.Color.ToUnityColor()
                        : UnityEngine.Color.Lerp(a.Color.ToUnityColor(),
                            b.Color.ToUnityColor(), Ease(0, 1));

                    spriteView.color = easedColor;
                }

                Canvas canvas = null;

                // Layer
                if (a.Layer != int.MinValue)
                {
                    a.Layer = Mathf.Clamp(a.Layer, 0, 2);
                    canvas = spriteView.GetComponent<Canvas>();
                    canvas.overrideSorting = true;
                    canvas.sortingLayerName = "Storyboard" + (a.Layer + 1);
                }

                // Order
                if (a.Order != int.MinValue)
                {
                    if (canvas == null) canvas = spriteView.GetComponent<Canvas>();
                    canvas.sortingOrder = a.Order;
                }
            }
        }

        protected virtual void UpdateControllers()
        {
            foreach (var controller in Controllers)
            {
                ControllerState a;
                ControllerState b;
                FindStates(controller.States, out a, out b);

                if (a != null)
                {
                    stateA = a;
                    stateB = b;
                    ease = a.Easing;

                    // Storyboard opacity
                    if (a.StoryboardOpacity != float.MinValue)
                    {
                        CanvasGroup.alpha = Ease(a.StoryboardOpacity, b.StoryboardOpacity);
                    }

                    // UI opacity
                    if (a.UiOpacity != float.MinValue)
                    {
                        if (UiCanvasGroup != null)
                            UiCanvasGroup.alpha = Ease(a.UiOpacity, b.UiOpacity);
                    }
                    
                    // Scanline opacity
                    if (a.ScanlineOpacity != float.MinValue)
                    {
                        ScanlineView.Instance.Opacity = Ease(a.ScanlineOpacity, b.ScanlineOpacity);
                    }

                    // Background dim
                    if (a.BackgroundDim != float.MinValue)
                    {
                        BackgroundOverlayMask.color =
                            BackgroundOverlayMask.color.WithAlpha(Ease(a.BackgroundDim, b.BackgroundDim));
                    }

                    // Note opacity
                    if (a.NoteOpacityMultiplier != float.MinValue)
                    {
                        var easedOpacity = Ease(a.NoteOpacityMultiplier, b.NoteOpacityMultiplier);
                        SimpleVisualOptions.Instance.GlobalOpacityMultiplier = easedOpacity;
                    }

                    // Scanline color
                    if (a.ScanlineColor != null)
                    {
                        var easedColor = b.ScanlineColor == null
                            ? a.ScanlineColor.ToUnityColor()
                            : UnityEngine.Color.Lerp(a.ScanlineColor.ToUnityColor(),
                                b.ScanlineColor.ToUnityColor(), Ease(0, 1));

                        ScanlineView.Instance.ColorOverride = easedColor;
                    }
                    
                    // Ring color
                    if (a.NoteRingColor != null)
                    {
                        var easedColor = b.NoteRingColor == null
                            ? a.NoteRingColor.ToUnityColor()
                            : UnityEngine.Color.Lerp(a.NoteRingColor.ToUnityColor(),
                                b.NoteRingColor.ToUnityColor(), Ease(0, 1));

                        SimpleVisualOptions.Instance.GlobalRingColorOverride = easedColor;
                    }

                    // Fill colors
                    if (a.NoteFillColors.Length > 0)
                    {
                        UnityEngine.Color[] easedColors = new UnityEngine.Color[10];
                        for (var i = 0; i < a.NoteFillColors.Length; i++)
                        {
                            if (a.NoteFillColors[i] == null)
                            {
                                easedColors[i] = UnityEngine.Color.clear;
                                continue;
                            }

                            if (b.NoteFillColors[i] != null)
                            {
                                easedColors[i] = UnityEngine.Color.Lerp(a.NoteFillColors[i].ToUnityColor(),
                                    b.NoteFillColors[i].ToUnityColor(), Ease(0, 1));
                            }
                            else
                            {
                                easedColors[i] = a.NoteFillColors[i].ToUnityColor();
                            }
                        }

                        SimpleVisualOptions.Instance.GlobalFillColorsOverride = easedColors;
                    }

                    if (!Testing && (Game.Instance is StoryboardGame || ShowEffects))
                    {
                        // Bloom

                        if (a.Bloom != null)
                        {
                            Sleek.settings.bloomEnabled = (bool) a.Bloom;
                            Sleek.enabled = Sleek.settings.bloomEnabled;
                            if ((bool) a.Bloom)
                            {
                                // Bloom Intensity
                                if (a.BloomIntensity != float.MinValue)
                                {
                                    Sleek.settings.bloomIntensity = Ease(a.BloomIntensity, b.BloomIntensity);
                                }
                            }
                        }

                        // Vignette

                        if (a.Vignette != null)
                        {
                            Prism.useVignette = (bool) a.Vignette;
                            if ((bool) a.Vignette)
                            {
                                // Vignette Intensity
                                if (a.VignetteIntensity != float.MinValue)
                                {
                                    Prism.vignetteStrength = Ease(a.VignetteIntensity, b.VignetteIntensity);
                                }

                                // Vignette Color
                                if (a.VignetteColor != null)
                                {
                                    Prism.vignetteColor = b.VignetteColor == null
                                        ? a.VignetteColor.ToUnityColor()
                                        : UnityEngine.Color.Lerp(a.VignetteColor.ToUnityColor(),
                                            b.VignetteColor.ToUnityColor(), Ease(0, 1));
                                }

                                // Vignette Start
                                if (a.VignetteStart != float.MinValue)
                                {
                                    Prism.vignetteStart = Ease(a.VignetteStart, b.VignetteStart);
                                }

                                // Vignette End
                                if (a.VignetteEnd != float.MinValue)
                                {
                                    Prism.vignetteEnd = Ease(a.VignetteEnd, b.VignetteEnd);
                                }
                            }
                        }

                        // Chromatic
                        if (a.Chromatic != null)
                        {
                            Prism.useChromaticAberration = (bool) a.Chromatic;
                            if ((bool) a.Chromatic)
                            {
                                // Chromatic Intensity
                                if (a.ChromaticIntensity != float.MinValue)
                                {
                                    Prism.chromaticIntensity = Ease(a.ChromaticIntensity, b.ChromaticIntensity);
                                }

                                // Chromatic Start
                                if (a.ChromaticStart != float.MinValue)
                                {
                                    Prism.chromaticDistanceOne = Ease(a.ChromaticStart, b.ChromaticStart);
                                }

                                // Chromatic End
                                if (a.ChromaticEnd != float.MinValue)
                                {
                                    Prism.chromaticDistanceTwo = Ease(a.ChromaticEnd, b.ChromaticEnd);
                                }
                            }
                        }

                        if (a.RadialBlur != null)
                        {
                            RadialBlur.enabled = (bool) a.RadialBlur;
                            if ((bool) a.RadialBlur)
                            {
                                if (a.RadialBlurIntensity != float.MinValue)
                                {
                                    RadialBlur.Intensity = Ease(a.RadialBlurIntensity, b.RadialBlurIntensity);
                                }
                            }
                        }

                        if (a.ColorAdjustment != null)
                        {
                            ColorAdjustment.enabled = (bool) a.ColorAdjustment;
                            if ((bool) a.ColorAdjustment)
                            {
                                if (a.Brightness != float.MinValue)
                                {
                                    ColorAdjustment.Brightness = Ease(a.Brightness, b.Brightness);
                                }

                                if (a.Saturation != float.MinValue)
                                {
                                    ColorAdjustment.Saturation = Ease(a.Saturation, b.Saturation);
                                }

                                if (a.Contrast != float.MinValue)
                                {
                                    ColorAdjustment.Contrast = Ease(a.Contrast, b.Contrast);
                                }
                            }
                        }

                        if (a.ColorFilter != null)
                        {
                            ColorFilter.enabled = (bool) a.ColorFilter;
                            if ((bool) a.ColorFilter)
                            {
                                if (a.ColorFilterColor != null)
                                {
                                    ColorFilter.ColorRGB = b.ColorFilterColor == null
                                        ? a.ColorFilterColor.ToUnityColor()
                                        : UnityEngine.Color.Lerp(a.ColorFilterColor.ToUnityColor(),
                                            b.ColorFilterColor.ToUnityColor(), Ease(0, 1));
                                }
                            }
                        }

                        if (a.GrayScale != null)
                        {
                            GrayScale.enabled = (bool) a.GrayScale;
                            if ((bool) a.GrayScale)
                            {
                                if (a.GrayScaleIntensity != float.MinValue)
                                {
                                    GrayScale._Fade = Ease(a.GrayScaleIntensity, b.GrayScaleIntensity);
                                }
                            }
                        }

                        if (a.Noise != null)
                        {
                            Noise.enabled = (bool) a.Noise;
                            if ((bool) a.Noise)
                            {
                                if (a.NoiseIntensity != float.MinValue)
                                {
                                    Noise.Noise = Ease(a.NoiseIntensity, b.NoiseIntensity);
                                }
                            }
                        }

                        if (a.Sepia != null)
                        {
                            Sepia.enabled = (bool) a.Sepia;
                            if ((bool) a.Sepia)
                            {
                                if (a.SepiaIntensity != float.MinValue)
                                {
                                    Sepia._Fade = Ease(a.SepiaIntensity, b.SepiaIntensity);
                                }
                            }
                        }

                        if (a.Dream != null)
                        {
                            Dream.enabled = (bool) a.Dream;
                            if ((bool) a.Dream)
                            {
                                if (a.DreamIntensity != float.MinValue)
                                {
                                    Dream.Distortion = Ease(a.DreamIntensity, b.DreamIntensity);
                                }
                            }
                        }

                        if (a.Fisheye != null)
                        {
                            Fisheye.enabled = (bool) a.Fisheye;
                            if ((bool) a.Fisheye)
                            {
                                if (a.FisheyeIntensity != float.MinValue)
                                {
                                    Fisheye.Distortion = Ease(a.FisheyeIntensity, b.FisheyeIntensity);
                                }
                            }
                        }

                        if (a.Shockwave != null)
                        {
                            Shockwave.enabled = (bool) a.Shockwave;
                            if ((bool) a.Shockwave)
                            {
                                if (a.ShockwaveSpeed != float.MinValue)
                                {
                                    Shockwave.Speed = Ease(a.ShockwaveSpeed, b.ShockwaveSpeed);
                                }
                            }
                            else
                            {
                                Shockwave.TimeX = 1.0f; // Reset shock wave position
                            }
                        }

                        if (a.Focus != null)
                        {
                            Focus.enabled = (bool) a.Focus;
                            if ((bool) a.Focus)
                            {
                                if (a.FocusIntensity != float.MinValue)
                                {
                                    Focus.Intensity = Ease(a.FocusIntensity, b.FocusIntensity);
                                }

                                if (a.FocusSize != float.MinValue)
                                {
                                    Focus.Size = Ease(a.FocusSize, b.FocusSize);
                                }

                                if (a.FocusSpeed != float.MinValue)
                                {
                                    Focus.Speed = Ease(a.FocusSpeed, b.FocusSpeed);
                                }

                                if (a.FocusColor != null)
                                {
                                    Focus.Color = b.FocusColor == null
                                        ? a.FocusColor.ToUnityColor()
                                        : UnityEngine.Color.Lerp(a.FocusColor.ToUnityColor(),
                                            b.FocusColor.ToUnityColor(),
                                            Ease(0, 1));
                                }
                            }
                        }

                        if (a.Glitch != null)
                        {
                            Glitch.enabled = (bool) a.Glitch;
                            if ((bool) a.Glitch)
                            {
                                if (a.GlitchIntensity != float.MinValue)
                                {
                                    Glitch.Glitch = Ease(a.GlitchIntensity, b.GlitchIntensity);
                                }
                            }
                        }

                        if (a.Artifact != null)
                        {
                            Artifact.enabled = (bool) a.Artifact;
                            if ((bool) a.Artifact)
                            {
                                if (a.ArtifactIntensity != float.MinValue)
                                {
                                    Artifact.Fade = Ease(a.ArtifactIntensity, b.ArtifactIntensity);
                                }

                                if (a.ArtifactColorisation != float.MinValue)
                                {
                                    Artifact.Colorisation = Ease(a.ArtifactColorisation, b.ArtifactColorisation);
                                }

                                if (a.ArtifactParasite != float.MinValue)
                                {
                                    Artifact.Parasite = Ease(a.ArtifactParasite, b.ArtifactParasite);
                                }

                                if (a.ArtifactNoise != null)
                                {
                                    Artifact.Noise = Ease(a.ArtifactNoise, b.ArtifactNoise);
                                }
                            }
                        }

                        if (a.Arcade != null)
                        {
                            Arcade.enabled = (bool) a.Arcade;
                            if ((bool) a.Arcade)
                            {
                                if (a.ArcadeIntensity != float.MinValue)
                                {
                                    Arcade.Fade = Ease(a.ArcadeIntensity, b.ArcadeIntensity);
                                }

                                if (a.ArcadeInterferanceSize != float.MinValue)
                                {
                                    Arcade.Interferance_Size = Ease(a.ArcadeInterferanceSize, b.ArcadeInterferanceSize);
                                }

                                if (a.ArcadeInterferanceSpeed != float.MinValue)
                                {
                                    Arcade.Interferance_Speed =
                                        Ease(a.ArcadeInterferanceSpeed, b.ArcadeInterferanceSpeed);
                                }

                                if (a.ArcadeContrast != float.MinValue)
                                {
                                    Arcade.Contrast = Ease(a.ArcadeContrast, b.ArcadeContrast);
                                }
                            }
                        }

                        if (a.Chromatical != null)
                        {
                            Chromatical.enabled = (bool) a.Chromatical;
                            if ((bool) a.Chromatical)
                            {
                                if (a.ChromaticalFade != float.MinValue)
                                {
                                    Chromatical.Fade = Ease(a.ChromaticalFade, b.ChromaticalFade);
                                }

                                if (a.ChromaticalIntensity != float.MinValue)
                                {
                                    Chromatical.Intensity = Ease(a.ChromaticalIntensity, b.ChromaticalIntensity);
                                }

                                if (a.ChromaticalSpeed != float.MinValue)
                                {
                                    Chromatical.Speed = Ease(a.ChromaticalSpeed, b.ChromaticalSpeed);
                                }
                            }
                            else
                            {
                                Chromatical.SetTimeX(1.0f);
                            }
                        }

                        if (a.Tape != null)
                        {
                            Tape.enabled = (bool) a.Tape;
                        }
                    }

                    var camera = Camera.main;

                    // X
                    if (a.X != float.MinValue)
                    {
                        camera.transform.SetX(EaseOrthographicX(a.X, b.X));
                    }

                    // Y
                    if (a.Y != float.MinValue)
                    {
                        camera.transform.SetY(EaseOrthographicY(a.Y, b.Y));
                    }

                    // RotX
                    if (a.RotX != float.MinValue)
                    {
                        var eulerAngles = camera.transform.eulerAngles;
                        eulerAngles.x = Ease(a.RotX, b.RotX);
                        camera.transform.eulerAngles = eulerAngles;
                    }

                    // RotY
                    if (a.RotY != float.MinValue)
                    {
                        var eulerAngles = camera.transform.eulerAngles;
                        eulerAngles.y = Ease(a.RotY, b.RotY);
                        camera.transform.eulerAngles = eulerAngles;
                    }

                    // RotZ
                    if (a.RotZ != float.MinValue)
                    {
                        var eulerAngles = camera.transform.eulerAngles;
                        eulerAngles.z = Ease(a.RotZ, b.RotZ);
                        camera.transform.eulerAngles = eulerAngles;
                    }

                    // Perspective
                    if (a.Perspective != null)
                    {
                        camera.orthographic = (bool) !a.Perspective;

                        if ((bool) a.Perspective)
                        {
                            // Fov
                            if (a.Fov != float.MinValue)
                            {
                                camera.fieldOfView = Ease(a.Fov, b.Fov);
                            }
                        }
                    }
                }
            }
        }

        protected virtual void UpdateTestViews()
        {
            Prism.useBloom = BloomPrismToggle.isOn;
            Prism.useVignette = VignettePrismToggle.isOn;
            Prism.useChromaticAberration = ChromaticToggle.isOn;
            Chromatical.enabled = ChromaticalToggle.isOn;
            RadialBlur.enabled = RadialBlurToggle.isOn;
            ColorAdjustment.enabled = ColorAdjustmentToggle.isOn;
            ColorFilter.enabled = ColorFilterCfpToggle.isOn;
            GrayScale.enabled = GrayScaleToggle.isOn;
            Noise.enabled = NoiseToggle.isOn;
            Sepia.enabled = SepiaToggle.isOn;
            Dream.enabled = DreamToggle.isOn;
            Fisheye.enabled = FisheyeToggle.isOn;
            Shockwave.enabled = ShockwaveToggle.isOn;
            Focus.enabled = FocusToggle.isOn;
            Glitch.enabled = GlitchToggle.isOn;
            Arcade.enabled = ArcadeToggle.isOn;
            Tape.enabled = TapeToggle.isOn;
            if (LowestResToggle.isOn)
            {
                Screen.SetResolution((int) (CytoidApplication.OriginalWidth * 0.5),
                    (int) (CytoidApplication.OriginalHeight * 0.5), true);
            }
            else if (LowerResToggle.isOn)
            {
                Screen.SetResolution((int) (CytoidApplication.OriginalWidth * 0.8),
                    (int) (CytoidApplication.OriginalHeight * 0.8), true);
            }
            else
            {
                Screen.SetResolution(CytoidApplication.OriginalWidth, CytoidApplication.OriginalHeight, true);
            }

            if (BloomSleekToggle.isOn || VignetteSleekToggle.isOn || ColorFilterSleekToggle.isOn)
            {
                Sleek.enabled = true;
                Sleek.settings.bloomEnabled = BloomSleekToggle.isOn;
                Sleek.settings.vignetteEnabled = VignetteSleekToggle.isOn;
                Sleek.settings.colorizeEnabled = ColorFilterSleekToggle.isOn;
            }
            else
            {
                Sleek.enabled = false;
            }
        }

        public void OnNoteClear(GameNote note)
        {
            foreach (var trigger in Triggers)
            {
                if (trigger.Type == TriggerType.NoteClear && trigger.Notes.Contains(note.Note.id))
                {
                    trigger.Triggerer = note;
                    OnTrigger(trigger);
                }

                if (trigger.Type == TriggerType.Combo && Game.Instance.Play.Combo == trigger.Combo)
                {
                    trigger.Triggerer = note;
                    OnTrigger(trigger);
                }

                if (trigger.Type == TriggerType.Score && Game.Instance.Play.Score >= trigger.Score)
                {
                    trigger.Triggerer = note;
                    OnTrigger(trigger);
                    Triggers.Remove(trigger);
                }
            }
        }

        public void OnTrigger(Trigger trigger)
        {
            // Spawn objects
            if (trigger.Spawn != null)
            {
                foreach (var id in trigger.Spawn)
                {
                    Spawn(id);
                }
            }

            // Destroy objects
            if (trigger.Destroy != null)
            {
                foreach (var id in trigger.Destroy)
                {
                    Destroy(id);
                }
            }

            // Destroy trigger if needed
            trigger.CurrentUses++;
            if (trigger.CurrentUses == trigger.Uses)
            {
                Triggers.Remove(trigger);
            }
        }

        public void Spawn(string id)
        {
            foreach (var child in Storyboard.Texts)
            {
                if (child.Id != id) continue;
                var text = child.Clone();
                RecalculateTime(text);
                Spawn(text);
                break;
            }

            foreach (var child in Storyboard.Sprites)
            {
                if (child.Id != id) continue;
                var sprite = child.Clone();
                RecalculateTime(sprite);
                StartCoroutine(Spawn(sprite));
                break;
            }
        }

        public void Destroy(string id)
        {
            var textToDestroy = TextViews.Where(it => it.Value.Id == id).Take(1).ToList();
            if (textToDestroy.Count > 0) Destroy(textToDestroy[0].Key.gameObject);
            var spriteToDestroy = SpriteViews.Where(it => it.Value.Id == id).Take(1).ToList();
            if (spriteToDestroy.Count > 0) Destroy(spriteToDestroy[0].Key.gameObject);
        }

        public void Spawn(Text text)
        {
            var textView = Instantiate(TextPrefab, Canvas.transform);
            TextViews[textView] = text;
        }

        public IEnumerator Spawn(Sprite sprite)
        {
            var spriteView = Instantiate(SpritePrefab, Canvas.transform);
            SpriteViews[spriteView] = sprite;

            var www = new WWW("file://" + Game.Instance.Level.BasePath + sprite.States[0].Path);
            yield return www;
            yield return null; // Wait an extra frame

            var texture = new Texture2D(4, 4, TextureFormat.DXT1, false);
            www.LoadImageIntoTexture(texture);

            var unitySprite =
                UnityEngine.Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0, 0));
            spriteView.GetComponent<Image>().sprite = unitySprite;

            www.Dispose();
        }

        public void RecalculateTime<T>(Object<T> obj) where T : ObjectState
        {
            var baseTime = Time;

            if (obj.States[0].Time != float.MaxValue)
            {
                baseTime = obj.States[0].Time;
            }
            else
            {
                obj.States[0].Time = baseTime;
            }

            var lastTime = baseTime;
            foreach (var state in obj.States)
            {
                if (state.RelativeTime != float.MinValue)
                {
                    state.Time = baseTime + state.RelativeTime;
                }

                if (state.AddTime != float.MinValue)
                {
                    state.Time = lastTime + state.AddTime;
                }

                lastTime = state.Time;
            }
        }

        private ObjectState stateA;
        private ObjectState stateB;
        private EasingFunction.Ease ease;

        private float Ease(float i, float j)
        {
            if (j == float.MinValue) return i;
            if (Time <= stateA.Time) return i;
            if (Time >= stateB.Time) return j;
            return EasingFunction.GetEasingFunction(ease)
                .Invoke(i, j, (Time - stateA.Time) / (stateB.Time - stateA.Time));
        }

        private float EaseCanvasX(float i, float j)
        {
            if (j == float.MinValue) return i;
            return Ease(i / 800 * CanvasRect.width, j / 800 * CanvasRect.width);
        }

        private float EaseCanvasY(float i, float j)
        {
            if (j == float.MinValue) return i;
            return Ease(i / 600 * CanvasRect.height, j / 600 * CanvasRect.height);
        }

        private float EaseOrthographicX(float i, float j)
        {
            if (j == float.MinValue) return i;
            return Ease(i * Camera.main.orthographicSize / Screen.height * Screen.width,
                j * Camera.main.orthographicSize / Screen.height * Screen.width);
        }

        private float EaseOrthographicY(float i, float j)
        {
            if (j == float.MinValue) return i;
            return Ease(i * Camera.main.orthographicSize, j * Camera.main.orthographicSize);
        }

        private void FindStates<T>(List<T> states, out T currentState, out T nextState) where T : ObjectState
        {
            if (states.Count == 0)
            {
                currentState = null;
                nextState = null;
                return;
            }

            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].Time > Time) // Next state
                {
                    // Current state is the previous state
                    currentState = i > 0 ? states[i - 1] : null;
                    nextState = states[i];
                    return;
                }
            }

            currentState = states.Last();
            nextState = currentState;
        }

        public bool Testing = false;

        public Toggle BloomPrismToggle;
        public Toggle BloomSleekToggle;
        public Toggle VignettePrismToggle;
        public Toggle VignetteSleekToggle;
        public Toggle ChromaticToggle;
        public Toggle ChromaticalToggle;
        public Toggle RadialBlurToggle;
        public Toggle ColorAdjustmentToggle;
        public Toggle ColorFilterCfpToggle;
        public Toggle ColorFilterSleekToggle;
        public Toggle GrayScaleToggle;
        public Toggle NoiseToggle;
        public Toggle SepiaToggle;
        public Toggle DreamToggle;
        public Toggle FisheyeToggle;
        public Toggle ShockwaveToggle;
        public Toggle FocusToggle;
        public Toggle GlitchToggle;
        public Toggle ArcadeToggle;
        public Toggle TapeToggle;
        public Toggle LowerResToggle;
        public Toggle LowestResToggle;
    }
}