using UnityEngine.UI;

public class TierIntroSplash : CleanTitleTransitionElement
{
    public Game game;
    public Text text;
    
    protected override void Awake()
    {
        base.Awake();
        game.onGameLoaded.AddListener(_ => OnGameLoaded());
    }

    public void OnGameLoaded()
    {
        if (game.State.Mode != GameMode.Tier) return;

        var introLabel = game.TierPlaySession?.IntroLabel;
        if (string.IsNullOrEmpty(introLabel)) return;

        text.text = introLabel;
        game.BeforeStartTasks.Add(Animate());
    }
}
