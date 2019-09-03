using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ParallaxElement : MonoBehaviour
{

    public int width = 1920;
    public int height = 1080;

    public float[] speeds = {200, 120, 180, 200, 75, 50};
    public float multiplier = -8f;

    private List<Layer> layers = new List<Layer>();
    
    private void Awake()
    {
        var index = 0;
        foreach (Transform child in transform)
        {
            var layer = new Layer {RectTransform = child.GetComponent<RectTransform>(), Index = index++};
            layer.OriginalPos = layer.RectTransform.anchoredPosition;
            layers.Add(layer);
        }
    }

    private void LateUpdate()
    {
        var normalizedMousePos = Input.mousePosition / new Vector2(UnityEngine.Screen.width, UnityEngine.Screen.height);
        var mousePos = normalizedMousePos * new Vector2(width, height) - new Vector2(width / 2.0f, height / 2.0f);

        foreach (var layer in layers)
        {
            var xPercentage = mousePos.x / speeds[layer.Index] / multiplier;
            var yPercentage = mousePos.y / speeds[layer.Index] / multiplier;
            layer.RectTransform.DOAnchorPos(new Vector2(layer.OriginalPos.x + xPercentage * width,
                layer.OriginalPos.y + yPercentage * height), 0.8f);
        }
    }

    public class Layer
    {
        public RectTransform RectTransform;
        public Vector2 OriginalPos;
        public int Index;
    }
}

