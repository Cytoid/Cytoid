using System.Collections.Generic;
using System.Linq;
using LeTai.Asset.TranslucentImage;
using Newtonsoft.Json;
using Polyglot;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuScreen : Screen
{
    public const string Id = "MainMenu";

    public RectTransform layout;
    public Text freePlayText;
    public InteractableMonoBehavior aboutButton;

    public Image upperLeftOverlayImage;
    public Image rightOverlayImage;
    
    public override string GetId() => Id;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        aboutButton.onPointerClick.AddListener(it => Dialog.PromptAlert($"<b>Cytoid {Context.Version}</b>\nThank you for playing!"));
    }

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();

        upperLeftOverlayImage.SetAlpha(Context.CharacterManager.GetActiveCharacterAsset().mainMenuUpperLeftOverlayAlpha);
        rightOverlayImage.SetAlpha(Context.CharacterManager.GetActiveCharacterAsset().mainMenuRightOverlayAlpha);
        
        freePlayText.text = "MAIN_LEVELS_LOADED".Get(Context.LevelManager.LoadedLocalLevels.Count(it => 
            it.Value.Type == LevelType.Community || it.Value.Type == LevelType.Official));
        freePlayText.transform.RebuildLayout();
        ProfileWidget.Instance.Enter();

        if (Context.CharacterManager.GetActiveCharacterAsset().mirrorLayout)
        {
            layout.anchorMin = new Vector2(0, 0.5f);
            layout.anchorMax = new Vector2(0, 0.5f);
            layout.pivot = new Vector2(0, 0.5f);
            layout.anchoredPosition = new Vector2(96, -90);
        }
        else
        {
            layout.anchorMin = new Vector2(1, 0.5f);
            layout.anchorMax = new Vector2(1, 0.5f);
            layout.pivot = new Vector2(1, 0.5f);
            layout.anchoredPosition = new Vector2(-96, -90);
        }
    }

}