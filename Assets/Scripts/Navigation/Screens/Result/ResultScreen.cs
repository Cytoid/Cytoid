using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Newtonsoft.Json.Linq;
using Proyecto26;
using RSG;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ResultScreen : Screen
{
    public const string Id = "Result";

    public TransitionElement lowerLeftColumn;
    public TransitionElement lowerRightColumn;
    public TransitionElement upperRightColumn;

    public InteractableMonoBehavior shareButton;
    public InteractableMonoBehavior nextButton;
    public InteractableMonoBehavior retryButton;

    public Text scoreText;
    public Text newBestText;
    public Text gradeText;
    public Text accuracyText;
    public Text maxComboText;
    public Text standardMetricText;
    public Text advancedMetricText;

    public GameObject rankingIcon;
    public SpinnerElement rankingSpinner;
    public Text rankingText;
    public RankingContainer rankingContainer;
    public Text rankingContainerStatusText;

    public SpinnerElement ratingSpinner;
    public Text ratingText;

    private GameResult result;

    public override string GetId() => Id;

    public override async void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        rankingContainerStatusText.text = "";
        result = Context.LastGameResult;
        Context.LastGameResult = null;
        if (result == null)
        {
            Debug.Log("Result not set, entering test mode...");
            await Context.LevelManager.LoadFromMetadataFiles(new List<string> {Context.DataPath + "/playeralice/level.json"});
            Context.SelectedLevel = Context.LevelManager.LoadedLevels[0];
            Context.SelectedDifficulty = Difficulty.Parse(Context.LevelManager.LoadedLevels[0].Meta.charts[0].type);
            Context.LocalPlayer.PlayRanked = true;
            result = new GameResult
            {
                Score = 999729,
                Accuracy = 0.99942353,
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
                ChartType = Difficulty.Hard
            };
        }

        // Load translucent cover
        var image = TranslucentCover.Instance.image;
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
        image.color = Color.white.WithAlpha(0);

        // Update performance info
        scoreText.text = result.Score.ToString("D6");
        accuracyText.text = (Math.Floor(result.Accuracy * 100 * 100) / 100).ToString("0.00") + "% accuracy";
        if (Math.Abs(result.Accuracy - 1) < 0.000001) accuracyText.text = "Full accuracy";
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

        // Increment local play count
        Context.LocalPlayer.SetPlayCount(result.LevelId, result.ChartType.Name,
            Context.LocalPlayer.GetPlayCount(result.LevelId, result.ChartType.Name) + 1);

        // Save performance to local
        var clearType = string.Empty;
        if (result.Mods.Contains(Mod.AP)) clearType = "AP";
        if (result.Mods.Contains(Mod.FC)) clearType = "FC";
        if (result.Mods.Contains(Mod.Hard)) clearType = "Hard";
        if (result.Mods.Contains(Mod.ExHard)) clearType = "ExHard";

        if (!Context.LocalPlayer.HasPerformance(result.LevelId, result.ChartType.Name, Context.LocalPlayer.PlayRanked))
        {
            newBestText.text = "NEW!";
            Context.LocalPlayer.SetBestPerformance(result.LevelId, result.ChartType.Name,
                Context.LocalPlayer.PlayRanked,
                new LocalPlayer.Performance
                {
                    Score = result.Score, Accuracy = (float) result.Accuracy, ClearType = clearType
                });
        }
        else
        {
            var historicBest = Context.LocalPlayer.GetBestPerformance(result.LevelId,
                result.ChartType.Name, Context.LocalPlayer.PlayRanked);
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

            Context.LocalPlayer.SetBestPerformance(result.LevelId, result.ChartType.Name,
                Context.LocalPlayer.PlayRanked, newBest);
        }
    }

    public override async void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();

        TranslucentCover.Instance.image.DOFade(0.7f, 0.8f);
        ProfileWidget.Instance.Enter();
        upperRightColumn.Enter();

        if (Context.OnlinePlayer.IsAuthenticated)
        {
            UploadRecord();
            UpdateRating();
        }
        else
        {
            EnterControls();
        }

        if (Context.LocalPlayer.PlayRanked && !Context.OnlinePlayer.IsAuthenticated)
        {
            UpdateRankings();
        }
        else
        {
            rankingIcon.SetActive(false);
        }
        
        Context.OnlinePlayer.onAuthenticated.AddListener(() =>
        {
            UpdateRankings();
            UpdateRating();
        });

        LoopAudioPlayer.Instance.PlayResultLoopAudio();
        LoopAudioPlayer.Instance.FadeInLoopPlayer(0);
    }

    public override void OnScreenBecameInactive()
    {
        base.OnScreenBecameInactive();
        Run.After(0.4f, () => TranslucentCover.Instance.image.color = Color.white.WithAlpha(0));
    }

    public void UploadRecord()
    {
        rankingSpinner.IsSpinning = true;
        var uri = $"{Context.ApiBaseUrl}/levels/{result.LevelId}/charts/{result.ChartType.Id}/records";
        Debug.Log("Posting to " + uri);
        RestClient.Post<UploadRecordResult>(new RequestHelper
        {
            Uri = uri,
            BodyString = JObject.FromObject(new UploadRecord
            {
                score = result.Score,
                accuracy = 0.998888, //result.Accuracy.ToString("0.00000000"),
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
        }).Then(_ => { Toast.Next(Toast.Status.Success, "Performance synchronized.".Localized()); }
        ).Catch(error =>
        {
            Debug.Log(error.Response);
            Toast.Next(Toast.Status.Failure, "This level is not ranked!".Localized());
        }).Finally(() =>
        {
            EnterControls();
            UpdateRankings();
        });
    }

    public void UpdateRankings()
    {
        rankingContainer.Clear();
        rankingSpinner.IsSpinning = true;
        rankingContainerStatusText.text = "Downloading level rankings...";
        Context.OnlinePlayer.GetLevelRankings(result.LevelId, result.ChartType.Id)
            .Then(ret =>
            {
                var (rank, entries) = ret;
                rankingContainer.SetData(entries);
                if (rank > 0)
                {
                    if (rank > 99) rankingText.text = "#99+";
                    else rankingText.text = "#" + rank;
                }
                else rankingText.text = "N/A";
                rankingContainerStatusText.text = "";
            })
            .Catch(error =>
            {
                Debug.LogError(error);
                rankingContainerStatusText.text = "Could not download level rankings.";
            })
            .Finally(() => rankingSpinner.IsSpinning = false);
    }

    public void UpdateRating()
    {
        ratingSpinner.IsSpinning = true;
        RestClient.Get<LevelRating>($"{Context.ApiBaseUrl}/levels/{result.LevelId}/ratings")
            .Then(levelRating => { ratingText.text = (levelRating.average / 2.0).ToString("0.00"); })
            .Catch(error =>
            {
                Debug.Log(error);
                ratingText.text = "N/A";
            }).Finally(() => ratingSpinner.IsSpinning = false);
    }

    public void Done()
    {
        LoopAudioPlayer.Instance.FadeOutLoopPlayer(0.4f);
        LoopAudioPlayer.Instance.PlayMainLoopAudio();

        Context.ScreenManager.ChangeScreen("GamePreparation", ScreenTransition.Out);
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
}