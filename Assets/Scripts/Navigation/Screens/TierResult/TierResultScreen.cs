using System;
using System.Collections;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class TierResultScreen : Screen
{
    public const string Id = "TierResult";

    public TransitionElement lowerLeftColumn;
    public TransitionElement lowerRightColumn;
    public TransitionElement upperRightColumn;

    public TierGradientPane gradientPane;
    public TierStageResultWidget[] stageResultWidgets;
    
    public Text scoreText;
    public Text newBestText;
    public Text gradeText;
    public Text accuracyText;
    public Text maxComboText;
    public Text standardMetricText;
    public Text advancedMetricText;

    public RankingsTab rankingsTab;

    public InteractableMonoBehavior shareButton;
    public CircleButton nextButton;
    public CircleButton retryButton;
    
    private TierState tierState;
    
    public override string GetId() => Id;

    public override void OnScreenDestroyed()
    {
        base.OnScreenDestroyed();
        Context.ScreenManager.RemoveHandler(this);
    }

    public override async void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        
        tierState = Context.TierState;
        if (tierState == null)
        {
            tierState = new TierState(MockData.Season.tiers[0])
            {
                Combo = 1,
                CurrentStageIndex = 0,
                Health = 1000.0,
                IsFailed = false,
                MaxCombo = 1,
                Stages = new[]
                {
                    new GameState()
                }
            };
            tierState.OnComplete();
        }
        Context.TierState = null;

        nextButton.State = CircleButtonState.Next;
        nextButton.StartPulsing();
        nextButton.interactableMonoBehavior.onPointerClick.SetListener(_ =>
        {
            Context.Haptic(HapticTypes.SoftImpact, true);
            nextButton.StopPulsing();
            Done();
        });
        retryButton.State = CircleButtonState.Retry;
        retryButton.interactableMonoBehavior.onPointerClick.SetListener(_ =>
        {
            Context.Haptic(HapticTypes.SoftImpact, true);
            retryButton.StopPulsing();
            Retry();
        });

        // Update performance info
        scoreText.text = (Mathf.Floor((float) tierState.Completion * 100 * 100) / 100).ToString("0.00") + "%";
        accuracyText.text = "RESULT_X_ACCURACY".Get((Math.Floor(tierState.AverageAccuracy * 100 * 100) / 100).ToString("0.00"));
        if (Mathf.Approximately((float) tierState.AverageAccuracy, 1))
        {
            accuracyText.text = "RESULT_FULL_ACCURACY".Get();
        }
        maxComboText.text = "RESULT_X_COMBO".Get(tierState.MaxCombo);
        if (tierState.GradeCounts[NoteGrade.Bad] == 0 && tierState.GradeCounts[NoteGrade.Miss] == 0)
        {
            maxComboText.text = "RESULT_FULL_COMBO".Get();
        }

        var scoreGrade = ScoreGrades.FromTierCompletion(tierState.Completion);
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

        standardMetricText.text = $"<b>Perfect</b> {tierState.GradeCounts[NoteGrade.Perfect]}    " +
                                  $"<b>Great</b> {tierState.GradeCounts[NoteGrade.Great]}    " +
                                  $"<b>Good</b> {tierState.GradeCounts[NoteGrade.Good]}    " +
                                  $"<b>Bad</b> {tierState.GradeCounts[NoteGrade.Bad]}    " +
                                  $"<b>Miss</b> {tierState.GradeCounts[NoteGrade.Miss]}";
        advancedMetricText.text = $"<b>Early</b> {tierState.EarlyCount}    " +
                                  $"<b>Late</b> {tierState.LateCount}    " +
                                  $"<b>{"RESULT_AVG_TIMING_ERR".Get()}</b> {tierState.AverageTimingError:0.000}s    " +
                                  $"<b>{"RESULT_STD_TIMING_ERR".Get()}</b> {tierState.StandardTimingError:0.000}s";
        if (!Context.Player.Settings.DisplayEarlyLateIndicators) advancedMetricText.text = "";

        newBestText.text = "";
        if (tierState.Completion >= 1)
        {
            if (tierState.Tier.completion < 1)
            {
                newBestText.text = "TIER_CLEARED".Get();
            }
            else
            {
                var historicBest = tierState.Tier.completion;
                var newBest = tierState.Completion;
                if (newBest > historicBest)
                {
                    tierState.Tier.completion = tierState.Completion; // Update cached tier json
                    newBestText.text =
                        $"+{(Mathf.FloorToInt((float) (newBest - historicBest) * 100 * 100) / 100f):0.00}%";
                }
            }
        }

        shareButton.onPointerClick.SetListener(_ => StartCoroutine(Share()));

        gradientPane.SetModel(tierState.Tier);
        for (var index = 0; index < Math.Min(tierState.Tier.Meta.parsedStages.Count, 3); index++)
        {
            var widget = stageResultWidgets[index];
            var level = tierState.Tier.Meta.parsedStages[index];
            var stageResult = tierState.Stages[index];
            widget.difficultyBall.SetModel(Difficulty.Parse(level.Meta.charts.Last().type), level.Meta.charts.Last().difficulty);
            widget.titleText.text = level.Meta.title;
            widget.performanceWidget.SetModel(new LevelRecord.Performance
            {
                Score = (int) stageResult.Score, Accuracy = stageResult.Accuracy
            });
        }
        
        ProfileWidget.Instance.Enter();
        upperRightColumn.Enter();

        UploadRecord();
    }

    private bool isSharing;

    public IEnumerator Share()
    {
        if (isSharing) yield break;
        // TODO: Refactor with ResultScreen.cs
        
        isSharing = true;
        Context.Haptic(HapticTypes.SoftImpact, true);
        Context.AudioManager.Get("Navigate1").Play(ignoreDsp: true);
        yield return new WaitForEndOfFrame();
        var screenshot = new Texture2D(UnityEngine.Screen.width, UnityEngine.Screen.height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, UnityEngine.Screen.width, UnityEngine.Screen.height), 0, 0);
        screenshot.Apply();
        var tmpPath = Path.Combine(Application.temporaryCachePath, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ".png");
        File.WriteAllBytes(tmpPath, screenshot.EncodeToPNG());
        Destroy(screenshot);
        
        // TODO
        var shareText = $"#cytoid [{tierState.Tier.Meta.name}]";

        new NativeShare()
            .AddFile(tmpPath)
            .SetText(shareText)
            .Share();

        isSharing = false;
    }

    public void Done()
    {
        Context.ScreenManager.ChangeScreen(Context.ScreenManager.PeekHistory(), ScreenTransition.Out, willDestroy: true,
            addTargetScreenToHistory: false);
        Context.AudioManager.Get("LevelStart").Play();
    }

    public async void Retry()
    {
        if (State == ScreenState.Inactive) return;
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

    private void EnterControls()
    {
        lowerLeftColumn.Enter();
        lowerRightColumn.Enter();
    }

    public void UploadRecord()
    {
        rankingsTab.spinner.IsSpinning = true;

        var uploadTierRecord = SecuredOperations.MakeTierRecord(tierState);
        SecuredOperations.UploadTierRecord(tierState, uploadTierRecord)
            .Then(stateChange =>
                {
                    Toast.Next(Toast.Status.Success, "TOAST_TIER_CLEARED".Get());
                    EnterControls();
                    rankingsTab.UpdateTierRankings(tierState.Tier.Id);
                    Context.OnlinePlayer.FetchProfile();

                    if (stateChange.rewards != null 
                        && stateChange.rewards.Any(it => it.Type == Reward.RewardType.Level || it.Type == Reward.RewardType.Character))
                    {
                        RewardOverlay.Show(stateChange.rewards);

                        if (stateChange.rewards.Any(
                            it => it.Type == Reward.RewardType.Level))
                        {
                            Context.Library.Fetch();
                        }
                    }
                }
            ).CatchRequestError(error =>
            {
                Debug.LogWarning(error.Response);
                if (error.IsNetworkError)
                {
                    Toast.Next(Toast.Status.Failure, "TOAST_CHECK_NETWORK_CONNECTION".Get());
                }
                else if (error.IsHttpError)
                {
                    if (error.StatusCode == 404)
                    {
                        Toast.Next(Toast.Status.Failure, "TOAST_TIER_NOT_FOUND".Get());
                    }
                    else if (error.StatusCode == 400)
                    {
                        Toast.Next(Toast.Status.Failure, "TOAST_TIER_VERIFICATION_FAILED".Get());
                    }
                    else if (error.StatusCode == 500)
                    {
                        Toast.Next(Toast.Status.Failure, "TOAST_SERVER_INTERNAL_ERROR".Get());
                    }
                    else
                    {
                        Toast.Next(Toast.Status.Failure, $"Status code: {error.StatusCode}".Get());
                    }
                }

                Context.Haptic(HapticTypes.Failure, true);
                var dialog = Dialog.Instantiate();
                dialog.Message = "DIALOG_RETRY_SYNCHRONIZE_TIER_PERFORMANCE".Get();
                dialog.UseProgress = false;
                dialog.UsePositiveButton = true;
                dialog.UseNegativeButton = true;
                dialog.OnPositiveButtonClicked = _ =>
                {
                    dialog.Close();
                    UploadRecord();
                };
                dialog.OnNegativeButtonClicked = _ =>
                {
                    dialog.Close();
                    EnterControls();
                    Context.OnlinePlayer.FetchProfile();
                };
                dialog.Open();
            });
    }
    
    public override void OnScreenChangeStarted(Screen from, Screen to)
    {
        base.OnScreenChangeStarted(from, to);
        if (from == this && to != null)
        {
            NavigationBackdrop.Instance.FadeBrightness(0, onComplete: () =>
            {
                TranslucentCover.Clear();
                NavigationBackdrop.Instance.FadeBrightness(1);
            });
        }
    }
    
    public override void OnScreenChangeFinished(Screen from, Screen to)
    {
        base.OnScreenChangeFinished(from, to);
        if (from == this)
        {
            // Dispose game cover
            Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.GameCover);
        }
    }
}
