using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ParallaxElement : MonoBehaviour, ScreenChangeListener
{
    public static bool UseGyroscope = true;
    public static float GyroscopeMultiplier = 9f;

    public bool Enabled { get; set; } = false;
    
    public int width = 1920;
    public int height = 1080;

    public float[] speeds = {200, 120, 180, 200, 75, 50};
    public float multiplier = -8f;

    private Vector2 baseGyroVector = Vector2.zero;
    private List<Layer> layers = new List<Layer>();
    private float extraMultiplier = 1f;

    private void Awake()
    {
        var index = 0;
        foreach (Transform child in transform)
        {
            var layer = new Layer {RectTransform = child.GetComponent<RectTransform>(), Index = index++};
            layer.OriginalPos = layer.RectTransform.anchoredPosition;
            layers.Add(layer);
        }

        Context.ScreenManager.AddHandler(this);
    }

    private void OnDestroy()
    {
        Context.ScreenManager.RemoveHandler(this);
    }

    private void LateUpdate()
    {
        Vector2 pos;
        if (UseGyroscope)
        {
            pos = GyroToPos() * (GyroscopeMultiplier * extraMultiplier);
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
                layer.OriginalPos.y + yPercentage * height), 0.8f);
        }
    }

    public void SetGyroscopeMultiplier(float m)
    {
        GyroscopeMultiplier = m;
        Debug.LogError(m);
    }

    private Vector2 GyroToPos()
    {
        var att = Input.gyro.attitude;
        var pos = new Vector2(att.x - baseGyroVector.x, att.y - baseGyroVector.y);
        return new Vector2(-pos.y * Context.ReferenceHeight / 2.0f, pos.x * Context.ReferenceWidth / 2.0f);
    }

    private void ResetBaseGyroVector()
    {
        var att = Input.gyro.attitude;
        baseGyroVector = new Vector2(att.x, att.y);
    }
    
    public void OnScreenChangeStarted(Screen from, Screen to)
    {
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