using LeTai.Asset.TranslucentImage;
using UnityEngine.UI;

public class MainMenuScreen : Screen
{
    private static bool startedMainLoop;
    public const string Id = "MainMenu";

    public Text freePlayText;
    public InteractableMonoBehavior aboutButton;
    
    public override string GetId() => Id;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        aboutButton.onPointerClick.AddListener(it =>
        {
            var dialog = Dialog.Instantiate();
            dialog.Message = "<b>Cytoid 2.0 Alpha 2</b>\nThank you for playing!";
            dialog.UseProgress = false;
            dialog.UsePositiveButton = true;
            dialog.UseNegativeButton = false;
            dialog.Open();
        });
    }

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        if (!startedMainLoop)
        {
            startedMainLoop = true;
            LoopAudioPlayer.Instance.PlayMainLoopAudio();
            LoopAudioPlayer.Instance.FadeInLoopPlayer();
        }
        TranslucentImageSource.Disabled = Context.LocalPlayer.GraphicsQuality == "low";

        freePlayText.text = $"{Context.LevelManager.LoadedLocalLevels.Count} LEVEL{(Context.LevelManager.LoadedLocalLevels.Count == 1 ? "" : "S")} LOADED";
        freePlayText.transform.RebuildLayout();
        ProfileWidget.Instance.Enter();
    }

}