using System;
using System.Collections;
using System.IO;
using System.Linq;
using MoreMountains.NiceVibrations;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class ResultScreen : Screen
{
    public const string Id = "Result";

    public TransitionElement lowerLeftColumn;
    public TransitionElement lowerRightColumn;
    public TransitionElement upperRightColumn;

    public GameObject infoContainer;
    public Text scoreText;
    public Text newBestText;
    public Text gradeText;
    public Text accuracyText;
    public Text maxComboText;
    public Text standardMetricText;
    public Text advancedMetricText;

    public RankingsTab rankingsTab;
    public RatingTab ratingTab;
    public GameObject rankingsIcon;
    public GameObject ratingIcon;

    public InteractableMonoBehavior shareButton;
    public CircleButton nextButton;
    public CircleButton retryButton;

    private bool uploadRecordSuccess;
    private GameState gameState;

    public override string GetId() => Id;

    public override async void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();

        gameState = Context.GameState;
        if (gameState == null)
        {
            // Test mode
            Debug.Log("Result not set, entering test mode...");
            
            gameState = new GameState(GameMode.Practice, Context.SelectedLevel, Context.SelectedDifficulty);
            Context.OnlinePlayer.OnAuthenticated.AddListener(() =>
            {
                rankingsTab.UpdateRankings(Context.SelectedLevel.Id, Context.SelectedDifficulty.Id);
                ratingTab.UpdateLevelRating(Context.SelectedLevel.Id);
                UploadRecord();
            });
        }

        Context.GameState = null;

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

        // Load translucent cover
        var path = "file://" + Context.SelectedLevel.Path + Context.SelectedLevel.Meta.background.path;
        TranslucentCover.Set(await Context.AssetMemory.LoadAsset<Sprite>(path, AssetTag.GameCover));
        NavigationBackdrop.Instance.Apply(it =>
        {
            it.IsBlurred = true;
            it.FadeBrightness(1, 0.8f);
        });

        // Update performance info
        scoreText.text = Mathf.FloorToInt((float) gameState.Score).ToString("D6");
        accuracyText.text =
            "RESULT_X_ACCURACY".Get((Math.Floor(gameState.Accuracy * 100 * 100) / 100).ToString("0.00"));
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

        var performance = new LevelRecord.Performance {Score = (int) gameState.Score, Accuracy = gameState.Accuracy};

        var record = gameState.Level.Record;

        // Increment local play count
        if (record.PlayCounts.ContainsKey(gameState.Difficulty.Id))
        {
            record.PlayCounts[gameState.Difficulty.Id] += 1;
        }
        else
        {
            record.PlayCounts[gameState.Difficulty.Id] = 1;
        }
        
        // Save performance to local
        var bestPerformances = gameState.Mode == GameMode.Standard
            ? record.BestPerformances
            : record.BestPracticePerformances;

        if (!bestPerformances.ContainsKey(gameState.Difficulty.Id))
        {
            newBestText.text = "RESULT_NEW".Get();
            bestPerformances[gameState.Difficulty.Id] = performance;
        }
        else
        {
            var historicBest = bestPerformances[gameState.Difficulty.Id];
            if (performance.Score > historicBest.Score)
            {
                newBestText.text = $"+{performance.Score - historicBest.Score}";
                bestPerformances[gameState.Difficulty.Id] = performance;
            }
            else if (performance.Score == historicBest.Score && performance.Accuracy > historicBest.Accuracy)
            {
                newBestText.text = $"+{(Mathf.FloorToInt((float) (performance.Accuracy - historicBest.Accuracy) * 100 * 100) / 100f):0.00}%";
                bestPerformances[gameState.Difficulty.Id] = performance;
            }
            else
            {
                newBestText.text = "";
            }
        }
        gameState.Level.SaveRecord();

        shareButton.onPointerClick.SetListener(_ => StartCoroutine(Share()));

        ProfileWidget.Instance.Enter();
        upperRightColumn.Enter();
        
        if (Context.IsOnline() && Context.OnlinePlayer.IsAuthenticated)
        {
            UploadRecord();
        }
        else
        {
            EnterControls();
        }

        if (Context.IsOnline())
        {
            rankingsIcon.SetActive(true);
            ratingIcon.SetActive(true);
            if (gameState.Mode == GameMode.Standard && !Context.OnlinePlayer.IsAuthenticated)
            {
                rankingsTab.UpdateRankings(gameState.Level.Id, gameState.Difficulty.Id);
            }
            ratingTab.UpdateLevelRating(gameState.Level.Id)
                .Then(it =>
                {
                    if (it == null) return;
                    if (Context.OnlinePlayer.IsAuthenticated && it.rating <= 0 &&
                        ScoreGrades.From(gameState.Score) >= ScoreGrade.A)
                    {
                        // Invoke the rate dialog
                        ratingTab.rateLevelElement.rateButton.onPointerClick.Invoke(null);
                    }
                });
        }
        else
        {
            rankingsIcon.SetActive(false);
            ratingIcon.SetActive(false);
        }
    }

    public override void OnScreenPostActive()
    {
        base.OnScreenPostActive();
        LayoutRebuilder.ForceRebuildLayoutImmediate(infoContainer.transform as RectTransform);
        infoContainer.GetComponentsInChildren<TransitionElement>().ForEach(it => it.UseCurrentStateAsDefault());
    }

    private bool isSharing;

    public IEnumerator Share()
    {
        if (isSharing) yield break;

        isSharing = true;
        Context.Haptic(HapticTypes.SoftImpact, true);
        Context.AudioManager.Get("Navigate3").Play(ignoreDsp: true);

        var levelMeta = Context.SelectedLevel.Meta;

        yield return new WaitForEndOfFrame();

        var screenshot = new Texture2D(UnityEngine.Screen.width, UnityEngine.Screen.height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, UnityEngine.Screen.width, UnityEngine.Screen.height), 0, 0);
        screenshot.Apply();

        var tmpPath = Path.Combine(Application.temporaryCachePath,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ".png");
        File.WriteAllBytes(tmpPath, screenshot.EncodeToPNG());

        Destroy(screenshot);

        var diff = Difficulty.ConvertToDisplayLevel(levelMeta.GetDifficultyLevel(Context.SelectedDifficulty.Id));
        var shareText = $"#cytoid [Lv.{diff}] {levelMeta.artist} - {levelMeta.title} / Charter: {levelMeta.charter}";
        if (uploadRecordSuccess)
        {
            shareText += $"\n{Context.WebsiteUrl}/levels/{levelMeta.id}";
        }

        new NativeShare()
            .AddFile(tmpPath)
            .SetText(shareText)
            .Share();

        isSharing = false;
    }

    public void UploadRecord()
    {
        var usedAuto =  gameState.Mods.Contains(Mod.Auto) || gameState.Mods.Contains(Mod.AutoDrag) || gameState.Mods.Contains(Mod.AutoHold) || gameState.Mods.Contains(Mod.AutoFlick);
        if (!Application.isEditor && usedAuto) throw new Exception();
        
        rankingsTab.spinner.IsSpinning = true;

        var uploadRecord = SecuredOperations.MakeRecord(gameState);
        SecuredOperations.UploadRecord(gameState, uploadRecord)
            .Then(stateChange =>
                {
                    uploadRecordSuccess = true;
                    Toast.Next(Toast.Status.Success, "TOAST_PERFORMANCE_SYNCHRONIZED".Get());
                    EnterControls();
                    rankingsTab.UpdateRankings(gameState.Level.Id, gameState.Difficulty.Id);
                    Context.OnlinePlayer.FetchProfile();
                    
                    if (stateChange.rewards != null && stateChange.rewards.Count > 0)
                    {
                        RewardOverlay.Show(stateChange.rewards);

                        if (stateChange.rewards.Any(
                            it => it.Type == OnlinePlayerStateChange.Reward.RewardType.Level))
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
                        Toast.Next(Toast.Status.Failure, "TOAST_LEVEL_NOT_RANKED".Get());
                    }
                    else if (error.StatusCode == 400)
                    {
                        Toast.Next(Toast.Status.Failure, "TOAST_LEVEL_VERIFICATION_FAILED".Get());   
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
                dialog.Message = "DIALOG_RETRY_SYNCHRONIZE_PERFORMANCE".Get();
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
                    rankingsTab.UpdateRankings(gameState.Level.Id, gameState.Difficulty.Id);
                    Context.OnlinePlayer.FetchProfile();
                };
                dialog.Open();
            });
    }

    public void Done()
    {
        if (State == ScreenState.Inactive) return;
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

        if (Context.Distribution == Distribution.China && Context.Player.ShouldOneShot("Tips: Calibration"))
        {
            Dialog.PromptAlert("<b>提示：</b>\n如果感觉手感不对劲的话，有可能需要设置谱面偏移。可以在准备界面的设置菜单中进入校正模式。");
        }
    }

    public override void OnScreenChangeStarted(Screen from, Screen to)
    {
        base.OnScreenChangeStarted(from, to);
        if (from == this)
        {
            NavigationBackdrop.Instance.FadeBrightness(0, onComplete: () =>
            {
                TranslucentCover.Clear();
                NavigationBackdrop.Instance.FadeBrightness(1);
            });
        }
    }
    
}