using System;
using System.Collections.Generic;
using UnityEngine;

public class HitSoundSelect : MonoBehaviour, ScreenBecameActiveListener
{
    public static readonly List<string> HitSounds = new List<string>
    {
        "none",
        "click1",
        "click2",
        "click3",
        "shaker",
        "tambourine",
        "rim",
        "hat",
        "clap",
        "donk",
        "8bit",
        "quack",
    };
    public static readonly List<string> HitSoundNames = new List<string>
    {
        "None",
        "Click 1",
        "Click 2",
        "Click 3",
        "Shaker",
        "Tambourine",
        "Rim",
        "Hat",
        "Clap",
        "Donk",
        "8-bit",
        "Quack",
    };

    [GetComponentInChildren] public CaretSelect select;

    public Game game;

    private void Awake()
    {
        if (game != null)
        {
            game.onGameLoaded.AddListener(_ => Load());
        }
    }

    public void OnScreenBecameActive()
    {
        Load();
    }

    public void Load()
    {
        if (select == null) select = GetComponentInChildren<CaretSelect>();
        select.labels = new List<string>(HitSoundNames);
        select.values = new List<string>(HitSounds);
        var lp = Context.LocalPlayer;
        select.Select(lp.HitSound, false, false);
        select.onSelect.RemoveAllListeners();
        select.onSelect.AddListener((_, it) =>
        {
            lp.HitSound = it;
            if (it != "none")
            {
                var audioClip = Resources.Load<AudioClip>("Audio/HitSounds/" + Context.LocalPlayer.HitSound);
                var hitSound = Context.AudioManager.Load("HitSound", audioClip, isResource: true);
                hitSound.Play();
            }
            else
            {
                Context.AudioManager.Unload("HitSound");
            }
        });
    }

}
