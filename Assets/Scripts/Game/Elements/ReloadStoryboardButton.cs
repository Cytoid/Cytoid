using System;

public class ReloadStoryboardButton : InteractableMonoBehavior
{
    public PlayerGame game;

    private void Awake()
    {
        onPointerClick.AddListener(_ => game.ReloadStoryboard());
    }
}