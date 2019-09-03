using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UniRx.Async;
using UnityEngine;

public class GameMessageText : MonoBehaviour
{

    public Game game;
    public TextMeshProUGUI tmp;
    
    public float fadeDuration = 0.4f;
    public Ease ease = Ease.OutCirc;
    
    public GameMessage CurrentMessage { get; protected set; }

    private List<Sequence> sequences = new List<Sequence>();
    
    protected void Awake()
    {
        game.onGameSpeedUp.AddListener(_ => Animate(new GameMessage
        {
            Type = GameMessage.AnimationType.Expand, 
            Color = Scanner.SpeedUpColor, 
            TextFunction = () => "SPEED UP"
        }));
        game.onGameSpeedDown.AddListener(_ => Animate(new GameMessage
        {
            Type = GameMessage.AnimationType.Shrink,
            Color = Scanner.SpeedDownColor,
            TextFunction = () => "SLOW DOWN"
        }));
        game.onGameWillUnpause.AddListener(async _ =>
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1.4f));
            Animate(new GameMessage
            {
                Type = GameMessage.AnimationType.Expand,
                Color = Color.white,
                TextFunction = () => "RESUMING IN " + Mathf.Max(0.1f, game.UnpauseCountdown).ToString("0.0") + "S",
                MaxSpacing = 96
            }, 2.1f);
        });
        tmp.color = Color.clear;
        tmp.text = "";
    }

    public async void Animate(GameMessage message, float duration = 1.5f)
    {
        CurrentMessage = message;
        tmp.color = message.Color.WithAlpha(0);
        tmp.text = message.TextFunction();
        tmp.characterSpacing = message.Type == GameMessage.AnimationType.Shrink ? message.MaxSpacing : message.MinSpacing;

        sequences.ForEach(it => it.Kill());
        sequences.Clear();
        DOTween.Sequence()
            .Append(
                DOTween.To(
                        () => tmp.color,
                        it => tmp.color = it,
                        tmp.color.WithAlpha(1),
                        fadeDuration
                    )
                    .SetEase(Ease.OutCubic)
            )
            .Append(
                DOTween.To(
                        () => tmp.color,
                        it => tmp.color = it,
                        tmp.color.WithAlpha(0),
                        fadeDuration
                    )
                    .SetDelay(duration - fadeDuration * 2)
                    .SetEase(Ease.OutCubic)
            )
            .Also(it => sequences.Add(it));
        DOTween.Sequence()
            .Append(
                DOTween.To(
                        () => tmp.characterSpacing,
                        it => tmp.characterSpacing = it,
                        message.Type == GameMessage.AnimationType.Expand ? message.MaxSpacing : message.MinSpacing,
                        duration
                    )
                    .SetEase(ease)
            )
            .Also(it => sequences.Add(it));

        await UniTask.Delay(TimeSpan.FromSeconds(duration));
        CurrentMessage = null;
    }

    public void Update()
    {
        if (CurrentMessage != null)
        {
            tmp.text = CurrentMessage.TextFunction();
        }
    }
}

public class GameMessage
{
    public AnimationType Type;
    public Color Color;
    public Func<string> TextFunction;
    public float MaxSpacing = 192f;
    public float MinSpacing;
    
    public enum AnimationType
    {
        Expand, Shrink, None
    }
}