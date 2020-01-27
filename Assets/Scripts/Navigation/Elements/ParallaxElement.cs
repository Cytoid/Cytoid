using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ParallaxElement : MonoBehaviour, ScreenChangeListener
{
    public static bool UseGyroscope = true;
    public static float GyroscopeMultiplier = 24f;

    public bool Enabled { get; set; } = true;

    public int width = 1920;
    public int height = 1080;

    public RectTransform menuTransform;
    public float menuSpeed;
    
    public float[] speeds = {200, 120, 180, 200, 75, 50};
    public float multiplier = -8f;

    private List<Layer> layers = new List<Layer>();
    private float extraMultiplier = 1f;

    private void Awake()
    {
        if (Application.platform == RuntimePlatform.Android) GyroscopeMultiplier = 24 * 1.8f;
        var index = 0;
        foreach (Transform child in transform)
        {
            var layer = new Layer {RectTransform = child.GetComponent<RectTransform>(), Index = index++};
            layer.OriginalPos = layer.RectTransform.anchoredPosition;
            layers.Add(layer);
        }
        var menuLayer = new Layer {RectTransform = menuTransform, Index = index++};
        menuLayer.OriginalPos = menuLayer.RectTransform.anchoredPosition;
        layers.Add(menuLayer);
        Array.Resize(ref speeds, speeds.Length + 1);
        speeds[speeds.Length - 1] = menuSpeed;
        
#if UNITY_EDITOR
        UseGyroscope = false;        
#endif

        Context.ScreenManager.AddHandler(this);
    }

    private void OnDestroy()
    {
        Context.ScreenManager.RemoveHandler(this);
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

        foreach (var layer in layers)
        {
            var xPercentage = pos.x / speeds[layer.Index] / multiplier;
            var yPercentage = pos.y / speeds[layer.Index] / multiplier;
            layer.RectTransform.DOAnchorPos(new Vector2(layer.OriginalPos.x + xPercentage * width,
                layer.OriginalPos.y + yPercentage * height), 0.4f);
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
}