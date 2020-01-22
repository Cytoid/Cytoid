using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DG.Tweening;
using Newtonsoft.Json.Linq;
using Proyecto26;
using RSG;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ResultScreen : Screen, ScreenChangeListener
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
    
    private GameResult result;

    public override string GetId() => Id;

    public override async void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        result = Context.LastGameResult;
        Context.LastGameResult = null;
        if (result == null)
        {
            // Test mode
            Debug.Log("Result not set, entering test mode...");
            await Context.LevelManager.LoadFromMetadataFiles(new List<string>
                {Context.DataPath + "/suconh_typex.alice/level.json"});
            Context.SelectedLevel = Context.LevelManager.LoadedLocalLevels.Values.First();
            Context.SelectedDifficulty = Difficulty.Parse(Context.LevelManager.LoadedLocalLevels.Values.First().Meta.charts[0].type);
            Context.LocalPlayer.PlayRanked = true;
            result = new GameResult
            {
                Score = 123,
                Accuracy = 1.942353,
                MaxCombo = 568,
                Mods = new List<Mod> {Mod.Fast},
                GradeCounts = new Dictionary<NoteGrade, int>
                {
                    {NoteGrade.Perfect, 563},
                    {NoteGrade.Great, 5},
                    {NoteGrade.Good, 0},
                    {NoteGrade.Bad, 0},
                    {NoteGrade.Miss, 0}
                },
                EarlyCount = 3,
                LateCount = 2,
                AverageTimingError = 0.0032,
                StandardTimingError = 0.0030,
                LevelId = "suconh_typex.alice",
                LevelVersion = 1,
                ChartType = Difficulty.Extreme
            };
            Context.OnlinePlayer.OnAuthenticated.AddListener(() =>
            {
                rankingsTab.UpdateRankings(result.LevelId, result.ChartType.Id);
                ratingTab.UpdateLevelRating(result.LevelId);
                UploadRecord();
            });
        }

        // Load translucent cover
        var image = TranslucentCover.Instance.image;
        image.color = Color.black;
        var sprite = Context.SpriteCache.GetCachedSprite("game://cover");
        if (sprite == null)
        {
            var path = "file://" + Context.SelectedLevel.Path + Context.SelectedLevel.Meta.background.path;
            using (var request = UnityWebRequestTexture.GetTexture(path))
            {
                await request.SendWebRequest();
                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError($"Failed to download cover from {path}");
                    Debug.LogError(request.error);
                    return;
                }

                sprite = DownloadHandlerTexture.GetContent(request).CreateSprite();
            }
        }

        image.sprite = sprite;
        image.FitSpriteAspectRatio();
        image.DOColor(Color.white.WithAlpha(0), 0.4f);

        // Update performance info
        scoreText.text = result.Score.ToString("D6");
        accuracyText.text = (Math.Floor(result.Accuracy * 100) / 100).ToString("0.00") + "% accuracy";
        if (Math.Abs(result.Accuracy - 100.0) < 0.000001) accuracyText.text = "Full accuracy";
        maxComboText.text = result.MaxCombo + " max combo";
        if (result.GradeCounts[NoteGrade.Bad] == 0 && result.GradeCounts[NoteGrade.Miss] == 0)
        {
            maxComboText.text = "Full combo";
        }

        var scoreGrade = ScoreGrades.From(result.Score);
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

        standardMetricText.text = $"<b>Perfect</b> {result.GradeCounts[NoteGrade.Perfect]}    " +
                                  $"<b>Great</b> {result.GradeCounts[NoteGrade.Great]}    " +
                                  $"<b>Good</b> {result.GradeCounts[NoteGrade.Good]}    " +
                                  $"<b>Bad</b> {result.GradeCounts[NoteGrade.Bad]}    " +
                                  $"<b>Miss</b> {result.GradeCounts[NoteGrade.Miss]}";
        advancedMetricText.text = $"<b>Early</b> {result.EarlyCount}    " +
                                  $"<b>Late</b> {result.LateCount}    " +
                                  $"<b>ATE</b> {result.AverageTimingError:0.000}s    " +
                                  $"<b>STE</b> {result.StandardTimingError:0.000}s";
        if (!Context.LocalPlayer.DisplayEarlyLateIndicators) advancedMetricText.text = "";

        // Increment local play count
        Context.LocalPlayer.SetPlayCount(result.LevelId, result.ChartType.Id,
            Context.LocalPlayer.GetPlayCount(result.LevelId, result.ChartType.Id) + 1);

        // Save performance to local
        var clearType = string.Empty;
        if (result.Mods.Contains(Mod.AP)) clearType = "AP";
        if (result.Mods.Contains(Mod.FC)) clearType = "FC";
        if (result.Mods.Contains(Mod.Hard)) clearType = "Hard";
        if (result.Mods.Contains(Mod.ExHard)) clearType = "ExHard";

        if (!Context.LocalPlayer.HasPerformance(result.LevelId, result.ChartType.Id, Context.LocalPlayer.PlayRanked))
        {
            newBestText.text = "NEW!";
            Context.LocalPlayer.SetBestPerformance(result.LevelId, result.ChartType.Id,
                Context.LocalPlayer.PlayRanked,
                new LocalPlayer.Performance
                {
                    Score = result.Score, Accuracy = (float) result.Accuracy, ClearType = clearType
                });
        }
        else
        {
            var historicBest = Context.LocalPlayer.GetBestPerformance(result.LevelId,
                result.ChartType.Id, Context.LocalPlayer.PlayRanked);
            var newBest = new LocalPlayer.Performance
            {
                Score = historicBest.Score, Accuracy = historicBest.Accuracy, ClearType = historicBest.ClearType
            };
            if (result.Score > historicBest.Score)
            {
                newBestText.text = $"+{result.Score - historicBest.Score}";
                newBest.Score = result.Score;
                newBest.ClearType = clearType;
            }
            else
            {
                newBestText.text = "";
            }

            if (result.Accuracy > historicBest.Accuracy)
            {
                newBest.Accuracy = (float) result.Accuracy;
            }

            Context.LocalPlayer.SetBestPerformance(result.LevelId, result.ChartType.Id,
                Context.LocalPlayer.PlayRanked, newBest);
        }

        await Resources.UnloadUnusedAssets();
    }

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();

        TranslucentCover.Instance.image.DOFade(0.7f, 0.8f);
        ProfileWidget.Instance.Enter();
        upperRightColumn.Enter();

        ratingTab.UpdateLevelRating(result.LevelId)
            .Then(it =>
            {
                if (it == null) return;
                if (it.rating <= 0 && ScoreGrades.From(result.Score) >= ScoreGrade.A)
                {
                    // Invoke the rate dialog
                    ratingTab.rateLevelElement.rateButton.onPointerClick.Invoke(null);
                }
            });

        if (Context.OnlinePlayer.IsAuthenticated)
        {
            UploadRecord();
        }
        else
        {
            EnterControls();
        }

        if (Context.LocalPlayer.PlayRanked && !Context.OnlinePlayer.IsAuthenticated)
        {
            rankingsTab.UpdateRankings(result.LevelId, result.ChartType.Id);
        }

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

    public void UploadRecord()
    {
        rankingsTab.spinner.IsSpinning = true;
        var uri = $"{Context.ApiBaseUrl}/levels/{result.LevelId}/charts/{result.ChartType.Id}/records";
        Debug.Log("Posting to " + uri);
        RestClient.Post<UploadRecordResult>(new RequestHelper
        {
            Uri = uri,
            BodyString = JObject.FromObject(new UploadRecord
            {
                score = result.Score,
                accuracy = double.Parse((result.Accuracy / 100.0).ToString("0.00000000")),
                details = new UploadRecord.Details
                {
                    perfect = result.GradeCounts[NoteGrade.Perfect],
                    great = result.GradeCounts[NoteGrade.Great],
                    good = result.GradeCounts[NoteGrade.Good],
                    bad = result.GradeCounts[NoteGrade.Bad],
                    miss = result.GradeCounts[NoteGrade.Miss],
                    maxCombo = result.MaxCombo
                },
                mods = result.Mods.Select(it => Enum.GetName(typeof(Mod), it)).ToList(),
                ranked = Context.LocalPlayer.PlayRanked,
            }).ToString(),
            Headers = Context.OnlinePlayer.GetJwtAuthorizationHeaders(),
            EnableDebug = true
        }).Then(_ =>
            {
                Toast.Next(Toast.Status.Success, "Performance synchronized.".Localized());
                EnterControls();
                rankingsTab.UpdateRankings(result.LevelId, result.ChartType.Id);
                Context.OnlinePlayer.FetchProfile();
            }
        ).Catch(error =>
        {
            Debug.Log(error.Response);
            if (error.IsNetworkError)
            {
                Toast.Next(Toast.Status.Failure, "Please check your network connection.");
            }
            else if (error.IsHttpError)
            {
                Toast.Next(Toast.Status.Failure, "This level is not ranked!".Localized());
            }

            var dialog = Dialog.Instantiate();
            dialog.Message = "Could not synchronize performance. Retry?";
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
                rankingsTab.UpdateRankings(result.LevelId, result.ChartType.Id);
                Context.OnlinePlayer.FetchProfile();
            };
            dialog.Open();
        });
    }

    public void Done()
    {
        LoopAudioPlayer.Instance.FadeOutLoopPlayer(0.4f);
        LoopAudioPlayer.Instance.PlayMainLoopAudio();
        Context.ScreenManager.ChangeScreen(GamePreparationScreen.Id, ScreenTransition.Out, willDestroy: true,
            onFinished: screen => Resources.UnloadUnusedAssets());
        Context.AudioManager.Get("LevelStart").Play(AudioTrackIndex.RoundRobin);
    }

    public async void RetryGame()
    {
        LoopAudioPlayer.Instance.FadeOutLoopPlayer(0.4f);

        State = ScreenState.Inactive;

        ProfileWidget.Instance.FadeOut();

        Context.AudioManager.Get("LevelStart").Play(AudioTrackIndex.RoundRobin);

        var sceneLoader = new SceneLoader("Game");
        sceneLoader.Load();
        await UniTask.Delay(TimeSpan.FromSeconds(0.8f));
        TranslucentCover.Instance.image.DOFade(0, 0.8f);
        await UniTask.Delay(TimeSpan.FromSeconds(0.8f));
        sceneLoader.Activate();
    }

    private void EnterControls()
    {
        lowerLeftColumn.Enter();
        lowerRightColumn.Enter();
    }

    public void OnScreenChangeStarted(Screen from, Screen to) => Expression.Empty();

    public void OnScreenChangeFinished(Screen from, Screen to)
    {
        if (from == this)
        {
            // Dispose game cover
            Context.SpriteCache.DisposeTagged("GameCover");
        }
    }
}