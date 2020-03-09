using UnityEngine.UI;

public class TierIntroSplash : AwaitableAnimatedElement
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
        if (game.State.Mode == GameMode.Tier && Context.TierState.CurrentStageIndex == 0)
        {
            text.text = Context.TierState.Tier.Meta.name;
            //game.BeforeStartTasks.Add(Animate());
        }
    }
}