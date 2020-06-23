using UnityEngine;
using UnityEngine.UI;

public class TranslucentCover : SingletonMonoBehavior<TranslucentCover>
{
    private Image image;
    private bool active;
    
    protected override void Awake()
    {
        base.Awake();
        image = GetComponentInChildren<Image>();
        image.enabled = false;
    }

    public static bool IsActive() => Instance.active;

    public static void Set(Sprite sprite)
    {
        Instance.active = true;
        var image = Instance.image;
        image.enabled = true;
        image.sprite = sprite;
        image.FitSpriteAspectRatio();
        NavigationBackdrop.Instance.ShouldParallaxActive = false;
        NavigationBackdrop.Instance.UpdateBlur();
    }
    
    public static void Clear()
    {
        Instance.active = false;
        var image = Instance.image;
        image.sprite = null;
        image.enabled = false;
        NavigationBackdrop.Instance.ShouldParallaxActive = true;
        NavigationBackdrop.Instance.UpdateBlur();
    }
    
}