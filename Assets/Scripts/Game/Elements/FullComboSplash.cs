using UnityEngine.UI;

public class FullComboSplash : AwaitableAnimatedElement
{
    public Game game;
    public Text text;
    
    protected override void Awake()
    {
        base.Awake();
        game.onGameCompleted.AddListener(_ => OnGameComplete());
    }

    public void OnGameComplete()
    {
        if (game.State.Combo == game.State.NoteCount && game.State.Score < 1000000)
        {
            text.text = game.State.Combo + "x";
            game.BeforeExitTasks.Add(Animate());
            Context.AudioManager.Get("LevelFullCombo").Play(AudioTrackIndex.RoundRobin);
        }
    }
}