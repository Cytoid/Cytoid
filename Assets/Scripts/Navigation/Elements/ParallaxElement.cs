using System.Collections.Generic;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class ParallaxElement : SerializedMonoBehaviour, ScreenChangeListener
{
    public static bool UseGyroscope = true;
    public static float GyroscopeMultiplier = 36f;
    public const float MenuSpeed = 100;

    public bool Enabled { get; set; } = true;
    public float CurrentScale { get; private set; } = 1f;
    public List<Layer> Layers => layers;
    
    public int width = 1920;
    public int height = 1080;

    public float minScale = 1f;
    public float maxScale = 1.2f;

    public List<float> speeds = new List<float> {200, 120, 180, 200, 75, 50};
    public float multiplier = -540f;

    public ParallaxAnimation animation = new ParallaxAnimation();

    private readonly List<Layer> layers = new List<Layer>();
    private Layer menuLayer;
    private float extraMultiplier = 1f;

    private Vector2 screenSize;
    
    private async void Awake()
    {
        await UniTask.WaitUntil(() => Context.ScreenManager != null);
        
        if (Application.platform == RuntimePlatform.Android) GyroscopeMultiplier = 24 * 1.8f;

        var index = 0;
        foreach (Transform child in transform)
        {
            var layer = new Layer {RectTransform = child.GetComponent<RectTransform>(), Index = index++};
            layer.OriginalPos = layer.RectTransform.anchoredPosition;
            layers.Add(layer);
        }
        
#if UNITY_EDITOR
        UseGyroscope = false;        
#endif

        Context.ScreenManager.AddHandler(this);
    }

    private void OnDestroy()
    {
        Context.ScreenManager.RemoveHandler(this);
    }

    private void Update()
    {
        animation.OnUpdate(this);

        var currentScreenSize = new Vector2(UnityEngine.Screen.width, UnityEngine.Screen.height);

        if (currentScreenSize != screenSize)
        {
            screenSize = currentScreenSize;
            
            const float minRatio = 19.5f / 9;
            const float maxRatio = 4f / 3;
            var ratio = currentScreenSize.x / currentScreenSize.y;
            CurrentScale = minScale + (maxScale - minScale) * ((ratio - minRatio) / (maxRatio - minRatio));
        }
        transform.SetLocalScale(CurrentScale);
    }

    private Vector2 gyroPos;

    private void LateUpdate()
    {
        if (!Enabled) return;

        Vector2 pos;
        if (UseGyroscope)
        {
            gyroPos += new Vector2(-Input.gyro.rotationRateUnbiased.y, -Input.gyro.rotationRateUnbiased.x);
            // print(gyroPos);
            gyroPos.x = Mathf.Clamp(gyroPos.x, -180f, 180f);
            gyroPos.y = Mathf.Clamp(gyroPos.y, -180f, 180f);
            pos = gyroPos * (GyroscopeMultiplier * extraMultiplier);
            // pos = GyroToPos() * (GyroscopeMultiplier * extraMultiplier);
        }
        else
        {
            var normalizedMousePos =
                Input.mousePosition / new Vector2(UnityEngine.Screen.width, UnityEngine.Screen.height);
            pos = normalizedMousePos * new Vector2(width, height) - new Vector2(width / 2.0f, height / 2.0f);
            pos *= extraMultiplier;
        }

        void HandleLayer(Layer layer, float speed)
        {
            if (speed == 0) return;
            var xPercentage = pos.x / speed / multiplier;
            var yPercentage = pos.y / speed / multiplier;
            layer.RectTransform.DOAnchorPos(new Vector2(layer.OriginalPos.x + xPercentage * width,
                layer.OriginalPos.y + yPercentage * height), 0.4f);
        }
        
        foreach (var layer in layers)
        {
            HandleLayer(layer, speeds[layer.Index]);
        }
        if (Context.ScreenManager.ActiveScreen is MainMenuScreen screen)
        {
            if (menuLayer == null)
            { 
                menuLayer = new Layer {RectTransform = screen.layout};
                menuLayer.OriginalPos = menuLayer.RectTransform.anchoredPosition;
            }
            HandleLayer(menuLayer, MenuSpeed);
        }
    }

    public void SetGyroscopeMultiplier(float m)
    {
        GyroscopeMultiplier = m;
        Debug.LogError(m);
    }

    private void ResetBaseGyroVector()
    {
        gyroPos = Vector2.zero;
    }

    public void OnScreenChangeStarted(Screen from, Screen to)
    {
        if (from is GamePreparationScreen)
        {
            Enabled = true;
        }
    }

    public void OnScreenChangeFinished(Screen from, Screen to)
    {
        if (to is InitializationScreen)
        {
            ResetBaseGyroVector();
        }

        if (to is MainMenuScreen)
        {
            // Reset gyroscope initial location
            ResetBaseGyroVector();
            extraMultiplier = 1f;
        }
        else if (to is GamePreparationScreen)
        {
            Enabled = false;
        }
        else
        {
            extraMultiplier = 0.5f;
        }
    }

    public class Layer
    {
        public RectTransform RectTransform;
        public Vector2 OriginalPos;
        public int Index;
    }

    public void Dispose()
    {
        GetComponentsInChildren<Image>().ForEach(it =>
        {
            it.sprite = null;
        });
    }
}

public class ParallaxAnimation
{
    public virtual void OnUpdate(ParallaxElement element)
    {
        
    }
}

public class ThreoseAnimation : ParallaxAnimation
{
    private bool initialized;
    private Image coloredBackgroundImage;
    private Image closedEyesImage;
    private bool shouldOpenEyes;
    
    public override void OnUpdate(ParallaxElement element)
    {
        if (!initialized)
        {
            coloredBackgroundImage = element.Layers[1].RectTransform.gameObject.GetComponent<Image>();
            closedEyesImage = element.Layers[4].RectTransform.gameObject.GetComponent<Image>();
            initialized = true;
        }

        var playbackTime = LoopAudioPlayer.Instance.PlaybackTime;
        var shouldOpenEyesNow =
            (playbackTime > 53.058 && playbackTime < 81.899)
            || (playbackTime > 149.761 && playbackTime < 247.597);

        if (shouldOpenEyesNow != shouldOpenEyes)
        {
            shouldOpenEyes = shouldOpenEyesNow;
            if (shouldOpenEyes)
            {
                coloredBackgroundImage.DOFade(1, 0.2f).SetEase(Ease.OutCubic);
                closedEyesImage.DOFade(0, 0.2f).SetEase(Ease.Linear);
            }
            else
            {
                coloredBackgroundImage.DOFade(0, 0.2f).SetEase(Ease.OutCubic);
                closedEyesImage.DOFade(1, 0.2f).SetEase(Ease.Linear);
            }
        }
    }
}