using System.Linq;
using DG.Tweening;
using LeTai.Asset.TranslucentImage;
using UnityEngine.UI;

public class MainMenuScreen : Screen
{
    public const string Id = "MainMenu";

    public TranslucentImage translucentImage;
    
    public override string GetId() => Id;
    
    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        translucentImage.SetAlpha(0);
    }

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        translucentImage.DOFade(0, 0.4f);
    }

    public override void OnScreenDestroyed()
    {
        base.OnScreenDestroyed();
    }

}