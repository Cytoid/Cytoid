using System;
using System.IO;
using Newtonsoft.Json;

public class ReloadPlayerButton : InteractableMonoBehavior
{
    public PlayerGame game;

    private void Awake()
    {
        onPointerClick.AddListener(_ =>
        {
            game.ReloadAll();
        });
    }
}