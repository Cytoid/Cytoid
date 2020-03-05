using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using DG.Tweening;
using Newtonsoft.Json.Linq;
using Proyecto26;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class TierBreakScreen : Screen, ScreenChangeListener
{
    public const string Id = "TierBreak";

    public GameObject infoContainer;
    public Text scoreText;
    public Text gradeText;
    public Text accuracyText;
    public Text maxComboText;
    public Text standardMetricText;
    public Text advancedMetricText;

    private GameState gameState;

    public override string GetId() => Id;

    public override async void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        // TODO: Most code here is the same as the one in ResultScreen.cs. Refactor?

        gameState = Context.GameState;
        Context.GameState = null;

        // Load translucent cover
        var image = TranslucentCover.Instance.image;
        image.color = Color.black;
        var sprite = Context.SpriteCache.GetCachedSpriteFromMemory("game://cover");
        image.sprite = sprite;
        image.FitSpriteAspectRatio();
        image.DOColor(Color.white.WithAlpha(0), 0.4f);

        // Update performance info
        scoreText.text = Mathf.FloorToInt((float) gameState.Score).ToString("D6");
        accuracyText.text = "RESULT_X_ACCURACY".Get((Math.Floor(gameState.Accuracy * 100) / 100).ToString("0.00"));
        if (Math.Abs(gameState.Accuracy - 100.0) < 0.000001)
        {
            accuracyText.text = "RESULT_FULL_ACCURACY".Get();
        }
        maxComboText.text = "RESULT_X_COMBO".Get(gameState.MaxCombo);
        if (gameState.GradeCounts[NoteGrade.Bad] == 0 && gameState.GradeCounts[NoteGrade.Miss] == 0)
        {
            maxComboText.text = "RESULT_FULL_COMBO".Get();
        }

        var scoreGrade = ScoreGrades.From(gameState.Score);
        gradeText.text = scoreGrade.ToString();
        gradeText.GetComponent<GradientMeshEffect>().SetGradient(scoreGrade.GetGradient());
        if (scoreGrade == ScoreGrade.MAX || scoreGrade == ScoreGrade.SSS)
        {
            scoreText.GetComponent<GradientMeshEffect>().SetGradient(scoreGrade.GetGradient());
        }
        else
        {
            scoreText.GetComponent<GradientMeshEffect>().enabled = false;
        }

        standardMetricText.text = $"<b>Perfect</b> {gameState.GradeCounts[NoteGrade.Perfect]}    " +
                                  $"<b>Great</b> {gameState.GradeCounts[NoteGrade.Great]}    " +
                                  $"<b>Good</b> {gameState.GradeCounts[NoteGrade.Good]}    " +
                                  $"<b>Bad</b> {gameState.GradeCounts[NoteGrade.Bad]}    " +
                                  $"<b>Miss</b> {gameState.GradeCounts[NoteGrade.Miss]}";
        advancedMetricText.text = $"<b>Early</b> {gameState.EarlyCount}    " +
                                  $"<b>Late</b> {gameState.LateCount}    " +
                                  $"<b>{"RESULT_AVG_TIMING_ERR".Get()}</b> {gameState.AverageTimingError:0.000}s    " +
                                  $"<b>{"RESULT_STD_TIMING_ERR".Get()}</b> {gameState.StandardTimingError:0.000}s";
        if (!Context.LocalPlayer.DisplayEarlyLateIndicators) advancedMetricText.text = "";

        await Resources.UnloadUnusedAssets();
    }

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();

        TranslucentCover.Instance.image.DOFade(0.9f, 0.8f);
        ProfileWidget.Instance.Enter();
      
        LoopAudioPlayer.Instance.PlayResultLoopAudio();
        LoopAudioPlayer.Instance.FadeInLoopPlayer(0);
    }

    public override void OnScreenPostActive()
    {
        base.OnScreenPostActive();
        LayoutRebuilder.ForceRebuildLayoutImmediate(infoContainer.transform as RectTransform);
        infoContainer.GetComponentsInChildren<TransitionElement>().ForEach(it => it.UseCurrentStateAsDefault());
    }

    public override void OnScreenBecameInactive()
    {
        base.OnScreenBecameInactive();
        Run.After(0.4f, () => TranslucentCover.Instance.image.color = Color.white.WithAlpha(0));
    }

    public async void NextStage()
    {
        LoopAudioPlayer.Instance.FadeOutLoopPlayer(0.4f);

        State = ScreenState.Inactive;

        ProfileWidget.Instance.FadeOut();

        Context.AudioManager.Get("LevelStart").Play();

        var sceneLoader = new SceneLoader("Game");
        sceneLoader.Load();
        await UniTask.Delay(TimeSpan.FromSeconds(0.8f));
        TranslucentCover.Instance.image.DOFade(0, 0.8f);
        await UniTask.Delay(TimeSpan.FromSeconds(0.8f));
        sceneLoader.Activate();
    }
    
    public void Finish()
    {
        Context.ScreenManager.ChangeScreen(TierResultScreen.Id, ScreenTransition.Out, willDestroy: true);
        Context.AudioManager.Get("LevelStart").Play();
    }

    public void OnScreenChangeStarted(Screen from, Screen to) => Expression.Empty();

    public void OnScreenChangeFinished(Screen from, Screen to)
    {
        if (from == this && !(to is TierResultScreen))
        {
            // Dispose game cover
            Context.SpriteCache.DisposeTaggedSpritesInMemory(SpriteTag.GameCover);
        }
    }
}