using System;
using System.Collections.Generic;
using System.Linq;
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
    public static readonly List<string> HitSoundNameKeys = new List<string>
    {
        "SETTINGS_NONE",
        "SETTINGS_HIT_SOUND_CLICK_1",
        "SETTINGS_HIT_SOUND_CLICK_2",
        "SETTINGS_HIT_SOUND_CLICK_3",
        "SETTINGS_HIT_SOUND_SHAKER",
        "SETTINGS_HIT_SOUND_TAMBOURINE",
        "SETTINGS_HIT_SOUND_RIM",
        "SETTINGS_HIT_SOUND_HAT",
        "SETTINGS_HIT_SOUND_CLAP",
        "SETTINGS_HIT_SOUND_DONK",
        "SETTINGS_HIT_SOUND_8_BIT",
        "SETTINGS_HIT_SOUND_QUACK",
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
        select.labels = new List<string>(HitSoundNameKeys.Select(it => it.Get()));
        select.values = new List<string>(HitSounds);
        var lp = Context.Player;
        select.Select(lp.Settings.HitSound, false, false);
        select.onSelect.RemoveAllListeners();
        select.onSelect.AddListener((_, it) =>
        {
            lp.Settings.HitSound = it;
            lp.SaveSettings();
            if (it != "none")
            {
                var audioClip = Resources.Load<AudioClip>("Audio/HitSounds/" + it);
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
