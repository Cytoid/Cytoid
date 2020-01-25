using UnityEngine.UI;

public class MainMenuScreen : Screen
{
    private static bool startedMainLoop;
    public const string Id = "MainMenu";

    public Text freePlayText;
    
    public override string GetId() => Id;

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        if (!startedMainLoop)
        {
            startedMainLoop = true;
            LoopAudioPlayer.Instance.PlayMainLoopAudio();
            LoopAudioPlayer.Instance.FadeInLoopPlayer();
        }

        freePlayText.text = $"{Context.LevelManager.LoadedLocalLevels.Count} LEVEL{(Context.LevelManager.LoadedLocalLevels.Count == 1 ? "" : "S")} LOADED";
        freePlayText.transform.RebuildLayout();
        ProfileWidget.Instance.Enter();
    }
}