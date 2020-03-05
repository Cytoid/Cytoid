using LeTai.Asset.TranslucentImage;
using Polyglot;
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
        if (!startedMainLoop)
        {
            startedMainLoop = true;
            LoopAudioPlayer.Instance.PlayMainLoopAudio();
            LoopAudioPlayer.Instance.FadeInLoopPlayer();
        }
        TranslucentImageSource.Disabled = Context.LocalPlayer.GraphicsQuality == "low";

        freePlayText.text = "MAIN_LEVELS_LOADED".Get(Context.LevelManager.LoadedLocalLevels.Count);
        freePlayText.transform.RebuildLayout();
        ProfileWidget.Instance.Enter();

        Context.SpriteCache.DisposeTaggedSpritesInMemory(SpriteTag.CharacterThumbnail);
    }

}