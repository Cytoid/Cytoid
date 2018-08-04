using System;
using System.Collections;
using System.Linq;
using System.Text;
using AppAdvisory.SharingSystem;
using Cytus2.Models;
using LunarConsolePluginInternal;
using Models;
using Newtonsoft.Json;
using QuickEngine.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameResultController : SingletonMonoBehavior<GameResultController>
{
    private string action = Action.Next;

    public static class Action
    {
        public const string Retry = "Retry";
        public const string Next = "Next";
    }

    [SerializeField] private Text titleText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text tpComboText;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button uploadButton;

    public bool IsUploading { get; private set; }
    public bool SuccessfullyUploaded { get; private set; }

    private void Start()
    {
        CytoidApplication.ResetResolution();

        IsUploading = false;
        SuccessfullyUploaded = false;

        BackgroundCanvasHelper.SetupBackgroundCanvas(gameObject.scene);

        // HIGHLIGHT
        Resources.UnloadUnusedAssets();
        // HIGHLIGHT

        if (OnlinePlayer.Authenticated)
        {
            StartCoroutine(AutoUpload());
        }
        else
        {
            uploadButton.gameObject.SetActive(false);
        }

        var play = CytoidApplication.CurrentPlay;

        var score = play.Score;
        var tp = play.Tp;
        var maxCombo = play.MaxCombo;

        titleText.text = CytoidApplication.CurrentLevel.title;

        var intScore = Mathf.FloorToInt((float) score);
        scoreText.text = intScore.ToString("D6");
        if (intScore == 1000000)
        {
            scoreText.color = Convert.HexToColor("#ffc107");
        } else if (intScore > 999000)
        {
            scoreText.color = Convert.HexToColor("#007bff");
        }

        var text = "";
        if (Math.Abs(tp - 100) < 0.000001) text += "Full accuracy";
        else text += (Math.Floor(tp * 100) / 100).ToString("0.00") + "% accuracy";
        text += " / ";
        if (maxCombo == play.NoteCleared) text += "Full combo";
        else text += maxCombo + " max combo";

        tpComboText.text = text;

        var info = string.Format(
            "<b>Perfect</b> {0}      <b>Great</b> {1}      <b>Good</b> {2}      <b>Bad</b> {3}      <b>Miss</b> {4}",
            play.NoteRankings.Values.Count(grading => grading == NoteGrade.Perfect),
            play.NoteRankings.Values.Count(grading => grading == NoteGrade.Great),
            play.NoteRankings.Values.Count(grading => grading == NoteGrade.Good),
            play.NoteRankings.Values.Count(grading => grading == NoteGrade.Bad),
            play.NoteRankings.Values.Count(grading => grading == NoteGrade.Miss)
        );

        if (PlayerPrefsExt.GetBool("early_late_indicator"))
        {
            info += string.Format("\n<alpha=#38>( <b>Early</b> {0}      <b>Late</b> {1}      <b>Average Timing Error</b> {2}{3:0.000}s      <b>Standard Timing Error</b> {4:0.000}s )",
                play.Early, play.Late, play.AvgTimeOff > 0 ? "+" : "", play.AvgTimeOff, play.StandardTimeOff);
        }
        infoText.text = info;

        DisplayDifficultyView.Instance.SetDifficulty(CytoidApplication.CurrentLevel, CytoidApplication.CurrentLevel.charts.Find(it => it.type == CytoidApplication.CurrentChartType));

        var ranked = CytoidApplication.CurrentRankedPlayData != null;

        // Save stats
        var oldScore = ZPlayerPrefs.GetFloat(PreferenceKeys.BestScore(CytoidApplication.CurrentLevel.id,
            CytoidApplication.CurrentChartType, ranked));
        var oldAccuracy = ZPlayerPrefs.GetFloat(PreferenceKeys.BestAccuracy(CytoidApplication.CurrentLevel.id,
            CytoidApplication.CurrentChartType, ranked));

        if (score > oldScore || ((int) score == (int) oldScore && tp > oldAccuracy))
        {
            EventKit.Broadcast("new best");
            
            ZPlayerPrefs.SetFloat(
                PreferenceKeys.BestScore(CytoidApplication.CurrentLevel.id, CytoidApplication.CurrentChartType, ranked),
                (float) score);
            ZPlayerPrefs.SetFloat(
                PreferenceKeys.BestAccuracy(CytoidApplication.CurrentLevel.id, CytoidApplication.CurrentChartType, ranked),
                (float) tp);
            var clearType = string.Empty;
            if (play.Mods.Contains(Mod.AP)) clearType = "AP";
            if (play.Mods.Contains(Mod.FC)) clearType = "FC";
            if (play.Mods.Contains(Mod.Hard)) clearType = "Hard";
            if (play.Mods.Contains(Mod.ExHard)) clearType = "ExHard";
            ZPlayerPrefs.SetString(
                PreferenceKeys.BestClearType(CytoidApplication.CurrentLevel.id, CytoidApplication.CurrentChartType,
                    ranked),
                clearType
            );
        }

        var playCount =
            ZPlayerPrefs.GetInt(
                PreferenceKeys.PlayCount(CytoidApplication.CurrentLevel.id, CytoidApplication.CurrentChartType),
                defaultValue: 0);

        ZPlayerPrefs.SetInt(
            PreferenceKeys.PlayCount(CytoidApplication.CurrentLevel.id, CytoidApplication.CurrentChartType),
            playCount + 1);
    }

    public IEnumerator AutoUpload()
    {
        uploadButton.interactable = false;
        
        yield return null;

        if (OnlinePlayer.Authenticated)
        {
            PostPlayData();
        }
    }

    public void SetAction(string action)
    {
        this.action = action;
    }

    public void DoAction()
    {
        BackgroundCanvasHelper.PersistBackgroundCanvas();

        switch (action)
        {
            case Action.Retry:
                SceneManager.LoadScene("CytusGame");
                break;
            case Action.Next:
                SceneManager.LoadScene("LevelSelection");
                break;
        }
    }

    public void PostPlayData()
    {
        if (!OnlinePlayer.Authenticated)
        {
            Popup.Make(this, "ERROR: You are not signed in.");
            return;
        }

        uploadButton.interactable = false;
        retryButton.interactable = false;
        nextButton.interactable = false;
        
        StartCoroutine(PostPlayDataCoroutine());
    }

    public IEnumerator PostPlayDataCoroutine()
    {
        if (IsUploading)
        {
            Popup.Make(this, "Already uploading.");
        }

        IsUploading = true;

        EventKit.Broadcast("reload rankings");

        Debug.Log("Posting play data");

        Popup.Make(this, "Uploading play data...");

        yield return OnlinePlayer.PostPlayData(CytoidApplication.CurrentPlay.IsRanked
            ? (IPlayData) CytoidApplication.CurrentRankedPlayData
            : CytoidApplication.CurrentUnrankedPlayData);

        switch (OnlinePlayer.LastPostResult.status)
        {
            case 200:
                Popup.Make(this, "Uploaded play data.");
                EventKit.Broadcast("profile update");
                SuccessfullyUploaded = true;
                break;
            case 400:
                Popup.Make(this, "ERROR: " + "Invalid play data.");
                uploadButton.interactable = true;
                SuccessfullyUploaded = false;
                break;
            case 401:
                Popup.Make(this, "ERROR: " + "You haven't signed in.");
                OnlinePlayer.Invalidate();
                uploadButton.interactable = true;
                SuccessfullyUploaded = false;
                break;
            default:
                Popup.Make(this, "ERROR: " + OnlinePlayer.LastPostResult.message);
                uploadButton.interactable = true;
                SuccessfullyUploaded = false;
                break;
        }

        IsUploading = false;
        retryButton.interactable = true;
        nextButton.interactable = true;
    }

    public void OnSharePressed()
    {
        VSSHARE.self.useCustomShareText = true;
        VSSHARE.self.customShareText = "#cytoid";
        VSSHARE.DOTakeScreenShot();
        VSSHARE.OnScreenshotTaken -= ShareScreenshot;
        VSSHARE.OnScreenshotTaken += ShareScreenshot;
    }

    public void OnUploadPressed()
    {
        if (IsUploading)
        {
            Popup.Make(this, "Already uploading.");
            return;
        }

        PostPlayData();
    }

    void ShareScreenshot(Texture2D tex)
    {
        VSSHARE.DOShareScreenshot(VSSHARE.self.customShareText, ShareType.Native);
    }
}