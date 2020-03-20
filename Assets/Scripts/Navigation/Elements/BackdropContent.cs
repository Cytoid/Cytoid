using UnityEngine;

[RequireComponent(typeof(CanvasGroup), typeof(TransitionElement))]
public class BackdropContent : MonoBehaviour
{
    public RectTransform content;
    public CanvasGroup contentCanvasGroup;
    
    public RectTransform target;

    public float a = 360;
    public float b;

    protected virtual void Update()
    {
        var alpha = (1 - target.anchoredPosition.y / a).Clamp(0, 1);
        var y = (target.anchoredPosition.y / a).Clamp(0, 1) * 240;
        contentCanvasGroup.alpha = alpha;
        content.SetLocalY(y);
    }

}