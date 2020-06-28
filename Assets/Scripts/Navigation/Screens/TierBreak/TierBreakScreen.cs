using System;
using System.Linq.Expressions;
using MoreMountains.NiceVibrations;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class TierBreakScreen : Screen
{
    public const string Id = "TierBreak";
    private const int IntermissionSeconds = 30;
    private static readonly int[] BroadcastAddTimes = {15, 10};

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
    private DateTimeOffset proceedToNextStageTime = DateTimeOffset.MaxValue;
    private DateTimeOffset nextBroadcastCountdownTime = DateTimeOffset.MaxValue;
    private int broadcastCount;

    public override string GetId() => Id;

    public override async void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        
        // TODO: Most code here is the same as the one in ResultScreen.cs. Refactor?

        gameState = Context.GameState;
        Context.GameState = null;

        // Load translucent cover
        var path = "file://" + gameState.Level.Path + gameState.Level.Meta.background.path;
        TranslucentCover.Set(await Context.AssetMemory.LoadAsset<Sprite>(path, AssetTag.GameCover));
     
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
        if (!Context.Player.Settings.DisplayEarlyLateIndicators) advancedMetricText.text = "";
        
        tierState = Context.TierState;
        modeText.text = tierState.Tier.Meta.name;

        var stage = tierState.CurrentStage;
        difficultyBall.SetModel(stage.Difficulty, stage.DifficultyLevel);
        stageTitleText.text = stage.Level.Meta.title;

        if (tierState.IsFailed)
        {
            retryButton.StartPulsing();
        }
        else
        {
            nextStageButton.StartPulsing();
        }
        
        nextStageButton.State = tierState.IsFailed ? CircleButtonState.GoBack : (tierState.IsCompleted ? CircleButtonState.Finish : CircleButtonState.NextStage);
        nextStageButton.interactableMonoBehavior.onPointerClick.SetListener(_ =>
        {
            Context.Haptic(HapticTypes.SoftImpact, true);
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
        retryButton.interactableMonoBehavior.onPointerClick.SetListener(_ =>
        {
            Context.Haptic(HapticTypes.SoftImpact, true);
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

        NavigationBackdrop.Instance.Apply(it =>
        {
            it.IsBlurred = true;
            it.FadeBrightness(1, 0.8f);
        });
        ProfileWidget.Instance.Enter();

        if (tierState.IsFailed)
        {
            Toast.Next(Toast.Status.Failure, "TOAST_TIER_FAILED".Get());
        }
        
        if (!tierState.IsCompleted && !tierState.IsFailed)
        {
            proceedToNextStageTime = DateTimeOffset.UtcNow.AddSeconds(IntermissionSeconds);
            nextBroadcastCountdownTime = DateTimeOffset.UtcNow;
        }
        else
        {
            proceedToNextStageTime = DateTimeOffset.MaxValue;
            nextBroadcastCountdownTime = DateTimeOffset.MaxValue;
        }
    }

    public override void OnScreenUpdate()
    {
        base.OnScreenUpdate();

        if (DateTimeOffset.UtcNow > proceedToNextStageTime)
        {
            nextStageButton.MockClick();
            nextStageButton.StopPulsing();
            NextStage();
        } 
        else if (DateTimeOffset.UtcNow > nextBroadcastCountdownTime)
        {
            Toast.Next(Toast.Status.Loading, "TOAST_TIER_PROCEEDING_TO_NEXT_STAGE_X".Get((int) Math.Ceiling((proceedToNextStageTime - DateTimeOffset.UtcNow).TotalSeconds)));
            if (broadcastCount >= BroadcastAddTimes.Length)
            {
                nextBroadcastCountdownTime = DateTimeOffset.MaxValue;
                return;
            }
            nextBroadcastCountdownTime = DateTimeOffset.UtcNow.AddSeconds(BroadcastAddTimes[broadcastCount++]);
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // Resuming?
        if (!pauseStatus && State == ScreenState.Active)
        {
            if (DateTimeOffset.UtcNow > proceedToNextStageTime)
            {
                // Fail!
                Dialog.PromptAlert("DIALOG_TIER_TIMED_OUT".Get());
                retryButton.StartPulsing();
        
                nextStageButton.State = CircleButtonState.GoBack;
                nextStageButton.interactableMonoBehavior.onPointerClick.AddListener(_ =>
                {
                    nextStageButton.StopPulsing();
                    GoBack();
                });
                retryButton.State = CircleButtonState.Retry;
                retryButton.interactableMonoBehavior.onPointerClick.AddListener(_ =>
                {
                    retryButton.StopPulsing();
                    Retry();
                });
                
                proceedToNextStageTime = DateTimeOffset.MaxValue;
            }
        }
    }
    
    public async void Retry()
    {
        if (State == ScreenState.Inactive) return;
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
        NavigationBackdrop.Instance.FadeBrightness(0, 0.8f);
        await UniTask.Delay(TimeSpan.FromSeconds(0.8f));
        if (!sceneLoader.IsLoaded) await UniTask.WaitUntil(() => sceneLoader.IsLoaded);
        sceneLoader.Activate();
    }

    public async void NextStage()
    {
        if (State == ScreenState.Inactive) return;
        State = ScreenState.Inactive;

        ProfileWidget.Instance.FadeOut();
        LoopAudioPlayer.Instance.StopAudio(0.4f);

        Context.AudioManager.Get("LevelStart").Play();

        var sceneLoader = new SceneLoader("Game");
        sceneLoader.Load();
        await UniTask.Delay(TimeSpan.FromSeconds(0.8f));
        NavigationBackdrop.Instance.FadeBrightness(0, 0.8f);
        await UniTask.Delay(TimeSpan.FromSeconds(0.8f));
        if (!sceneLoader.IsLoaded) await UniTask.WaitUntil(() => sceneLoader.IsLoaded);
        
        Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.GameCover);
        
        sceneLoader.Activate();
    }
    
    public void GoBack()
    {
        NavigationBackdrop.Instance.FadeBrightness(0, onComplete: () =>
        {
            TranslucentCover.Clear();
            NavigationBackdrop.Instance.FadeBrightness(1);
        });

        Context.TierState = null;
        Context.ScreenManager.ChangeScreen(TierSelectionScreen.Id, ScreenTransition.Out, willDestroy: true);
        Context.AudioManager.Get("LevelStart").Play();
    }
    
    public void Finish()
    {
        Context.ScreenManager.ChangeScreen(TierResultScreen.Id, ScreenTransition.Out, willDestroy: true, addTargetScreenToHistory: false);
        Context.AudioManager.Get("LevelStart").Play();
    }

    public override void OnScreenChangeFinished(Screen from, Screen to)
    {
        base.OnScreenChangeFinished(from, to);
        if (from == this && to is TierSelectionScreen)
        {
            // Go back to tier selection, so clear game cover
            Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.GameCover);
        }
    }
}