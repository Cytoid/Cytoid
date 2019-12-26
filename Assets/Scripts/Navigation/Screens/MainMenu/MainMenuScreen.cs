public class MainMenuScreen : Screen
{
    private static bool StartedMainLoop = false;
    public const string Id = "MainMenu";

    public override string GetId() => Id;

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        if (!StartedMainLoop)
        {
            StartedMainLoop = true;
            LoopAudioPlayer.Instance.PlayMainLoopAudio();
            LoopAudioPlayer.Instance.FadeInLoopPlayer();
        }

        ProfileWidget.Instance.Enter();
    }
}