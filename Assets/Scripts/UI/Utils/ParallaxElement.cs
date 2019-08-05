using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ParallaxElement : MonoBehaviour
{

    public int width = 1920;
    public int height = 1080;

    public float speed = -8f;

    private List<Layer> layers = new List<Layer>();
    
    private void Awake()
    {
        float[] speeds = {200, 120, 180, 200, 75, 50};
        var index = 0;
        foreach (Transform child in transform)
        {
            var layer = new Layer {rectTransform = child.GetComponent<RectTransform>(), speed = speeds[index++]};
            layer.originalPos = layer.rectTransform.anchoredPosition;
            layers.Add(layer);
        }
    }

    private void LateUpdate()
    {
        var normalizedMousePos = Input.mousePosition / new Vector2(UnityEngine.Screen.width, UnityEngine.Screen.height);
        var mousePos = normalizedMousePos * new Vector2(width, height) - new Vector2(width / 2.0f, height / 2.0f);

        foreach (var layer in layers)
        {
            var xPercentage = mousePos.x / layer.speed / speed;
            var yPercentage = mousePos.y / layer.speed / speed;
            layer.rectTransform.DOAnchorPos(new Vector2(layer.originalPos.x + xPercentage * width,
                layer.originalPos.y + yPercentage * height), 0.8f);
        }
    }

    public class Layer
    {
        public RectTransform rectTransform;
        public Vector2 originalPos;
        public float speed;
    }
}

