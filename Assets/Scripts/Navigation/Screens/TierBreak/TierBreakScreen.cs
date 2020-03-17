using System;
using System.Linq.Expressions;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class TierBreakScreen : Screen, ScreenChangeListener
{
    public const string Id = "TierBreak";

    public Text modeText;
    
    public DifficultyBall difficultyBall;
    public Text stageTitleText;
    
    public Text scoreText;
    public Text gradeText;
    public Text accuracyText;
    public Text maxComboText;
    public Text standardMetricText;
    public Text advancedMetricText;
    public GameObject criterionEntryHolder;
    public CircleButton nextStageButton;
    public CircleButton retryButton;
    
    public GameObject criterionEntryPrefab;
    private TierState tierState;
    private GameState gameState;

    public override string GetId() => Id;

    public override async void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        Context.ScreenManager.AddHandler(this);
        // TODO: Most code here is the same as the one in ResultScreen.cs. Refactor?

        gameState = Context.GameState;
        Context.GameState = null;

        // Load translucent cover
        TranslucentCover.LightMode();
        var path = "file://" + gameState.Level.Path + gameState.Level.Meta.background.path;
        TranslucentCover.SetSprite(await Context.AssetMemory.LoadAsset<Sprite>(path, AssetTag.GameCover));
     
        // Update performance info
        scoreText.text = Mathf.FloorToInt((float) gameState.Score).ToString("D6");
        accuracyText.text = "RESULT_X_ACCURACY".Get((Math.Floor(gameState.Accuracy * 100 * 100) / 100).ToString("0.00"));
        if (Mathf.Approximately((float) gameState.Accuracy, 1))
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
        
        // TIER START!!!
        // =====================
        
        tierState = Context.TierState;
        modeText.text = tierState.Tier.Meta.name;

        var stage = tierState.CurrentStage;
        difficultyBall.SetModel(stage.Difficulty, stage.DifficultyLevel);
        stageTitleText.text = stage.Level.Meta.title;

        print("ss: " +nextStageButton.scheduledPulse.startPulsingOnScreenBecameActive);
        print(nextStageButton.scheduledPulse.NextPulseTime);
        if (tierState.IsFailed)
        {
            print("Failed");
            retryButton.StartPulsing();
            print(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "/" + retryButton.scheduledPulse.NextPulseTime);
        }
        else
        {
            print("Not failed");
            nextStageButton.StartPulsing();
            print(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "/" + retryButton.scheduledPulse.NextPulseTime);
        }
        
        nextStageButton.State = tierState.IsFailed ? CircleButtonState.GoBack : (tierState.IsCompleted ? CircleButtonState.Finish : CircleButtonState.NextStage);
        nextStageButton.interactableMonoBehavior.onPointerClick.AddListener(_ =>
        {
            nextStageButton.StopPulsing();
            if (tierState.IsFailed)
            {
                GoBack();
            }
            else
            {
                if (tierState.IsCompleted) Finish();
                else NextStage();
            }
        });
        retryButton.State = CircleButtonState.Retry;
        retryButton.interactableMonoBehavior.onPointerClick.AddListener(_ =>
        {
            retryButton.StopPulsing();
            Retry();
        });
        
        // Update criterion entries
        foreach (Transform child in criterionEntryHolder.transform) Destroy(child.gameObject);
        
        foreach (var criterion in Context.TierState.Criteria)
        {
            var entry = Instantiate(criterionEntryPrefab, criterionEntryHolder.transform)
                .GetComponent<CriterionEntry>();
            entry.SetModel(criterion.Description, criterion.Judge(Context.TierState));
        }
        
        criterionEntryHolder.transform.RebuildLayout();

        await Resources.UnloadUnusedAssets();
    }
    
    public override void OnScreenDestroyed()
    {
        base.OnScreenDestroyed();
        Context.ScreenManager.RemoveHandler(this);
    }

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();

        TranslucentCover.Show(0.9f);
        ProfileWidget.Instance.Enter();
    }
    
    public async void Retry()
    {
        // TODO: Refactor with TierResult?
        State = ScreenState.Inactive;

        ProfileWidget.Instance.FadeOut();
        LoopAudioPlayer.Instance.StopAudio(0.4f);

        Context.AudioManager.Get("LevelStart").Play();
        Context.SelectedGameMode = GameMode.Tier;
        Context.TierState = new TierState(tierState.Tier);

        var sceneLoader = new SceneLoader("Game");
        sceneLoader.Load();
        await UniTask.Delay(TimeSpan.FromSeconds(0.8f));
        TranslucentCover.Hide();
        await UniTask.Delay(TimeSpan.FromSeconds(0.8f));
        sceneLoader.Activate();
    }

    public async void NextStage()
    {
        State = ScreenState.Inactive;

        ProfileWidget.Instance.FadeOut();
        LoopAudioPlayer.Instance.StopAudio(0.4f);

        Context.AudioManager.Get("LevelStart").Play();

        var sceneLoader = new SceneLoader("Game");
        sceneLoader.Load();
        await UniTask.Delay(TimeSpan.FromSeconds(0.8f));
        TranslucentCover.Hide();
        await UniTask.Delay(TimeSpan.FromSeconds(0.8f));
        
        Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.GameCover);
        
        sceneLoader.Activate();
    }
    
    public void GoBack()
    {
        TranslucentCover.Hide();
        
        Context.ScreenManager.ChangeScreen(TierSelectionScreen.Id, ScreenTransition.Out, willDestroy: true,
            onFinished: screen => Resources.UnloadUnusedAssets());
        Context.AudioManager.Get("LevelStart").Play();
    }
    
    public void Finish()
    {
        Context.ScreenManager.ChangeScreen(TierResultScreen.Id, ScreenTransition.Out, willDestroy: true, addToHistory: false);
        Context.AudioManager.Get("LevelStart").Play();
    }

    public void OnScreenChangeStarted(Screen from, Screen to) => Expression.Empty();

    public void OnScreenChangeFinished(Screen from, Screen to)
    {
        if (from == this && to is TierSelectionScreen)
        {
            // Go back to tier selection, so clear game cover
            Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.GameCover);
        }
    }
}