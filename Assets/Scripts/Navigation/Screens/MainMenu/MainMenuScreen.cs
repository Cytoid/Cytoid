using LeTai.Asset.TranslucentImage;
using Polyglot;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuScreen : Screen
{
    public const string Id = "MainMenu";

    public RectTransform layout;
    public Text freePlayText;
    public InteractableMonoBehavior aboutButton;
    
    public override string GetId() => Id;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        aboutButton.onPointerClick.AddListener(it =>
        {
            var dialog = Dialog.Instantiate();
            dialog.Message = $"<b>Cytoid {Context.Version}</b>\nThank you for playing!";
            dialog.UseProgress = false;
            dialog.UsePositiveButton = true;
            dialog.UseNegativeButton = false;
            dialog.Open();
        });
    }

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        TranslucentImageSource.Disabled = Context.LocalPlayer.GraphicsQuality == "low";

        freePlayText.text = "MAIN_LEVELS_LOADED".Get(Context.LevelManager.LoadedLocalLevels.Count);
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