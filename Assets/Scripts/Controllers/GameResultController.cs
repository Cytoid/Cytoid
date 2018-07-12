using System;
using System.Collections;
using System.Linq;
using System.Text;
using AppAdvisory.SharingSystem;
using LunarConsolePluginInternal;
using Newtonsoft.Json;
using QuickEngine.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameResultController : MonoBehaviour
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
	
	private void Start()
	{
		CytoidApplication.ResetResolution();
		
		IsUploading = false;
		
		BackgroundCanvasHelper.SetupBackgroundCanvas(gameObject.scene);
		
		// HIGHLIGHT
		Resources.UnloadUnusedAssets();
		// HIGHLIGHT

		StartCoroutine(AutoUpload());
		
		var play = CytoidApplication.CurrentPlay;

		var score = play.Score;
		var tp = play.Tp;
		var maxCombo = play.MaxCombo;

		titleText.text = CytoidApplication.CurrentLevel.title;

		var intScore = Mathf.CeilToInt((float) score);
		scoreText.text = intScore.ToString("D6");
		if (intScore == 1000000)
		{
			scoreText.color = Convert.HexToColor("#FDE74C");
		}
		
		var text = "";
		if (Math.Abs(tp - 100) < 0.000001) text += "Full accuracy";
		else text += tp.ToString("0.##") + "% accuracy";
		text += " / ";
		if (maxCombo == play.NoteCleared) text += "Full combo";
		else text += maxCombo + " max combo";

		tpComboText.text = text;
		
		var info = string.Format("<b>Perfect </b> {0}      <b>Great </b> {1}      <b>Good </b> {2}      <b>Bad </b> {3}      <b>Miss </b> {4}", 
			play.NoteRankings.Values.Count(grading => grading == NoteGrading.Perfect), 
			play.NoteRankings.Values.Count(grading => grading == NoteGrading.Great),
			play.NoteRankings.Values.Count(grading => grading == NoteGrading.Good), 
			play.NoteRankings.Values.Count(grading => grading == NoteGrading.Bad), 
			play.NoteRankings.Values.Count(grading => grading == NoteGrading.Miss)
		);
		infoText.text = info;
		
		DisplayDifficultyView.Instance.SetDifficulty(CytoidApplication.CurrentChartType, CytoidApplication.CurrentLevel.GetDifficulty(CytoidApplication.CurrentChartType));

		var ranked = CytoidApplication.CurrentRankedModeData != null;
		
		// Save stats
		var oldScore = ZPlayerPrefs.GetFloat(PreferenceKeys.BestScore(CytoidApplication.CurrentLevel,
			CytoidApplication.CurrentChartType, ranked));
		var oldTp = ZPlayerPrefs.GetFloat(PreferenceKeys.BestAccuracy(CytoidApplication.CurrentLevel,
			CytoidApplication.CurrentChartType, ranked));

		if (score > oldScore)
		{
			ZPlayerPrefs.SetFloat(PreferenceKeys.BestScore(CytoidApplication.CurrentLevel, CytoidApplication.CurrentChartType, ranked), (float) score);
		}
		if (tp > oldTp)
		{
			ZPlayerPrefs.SetFloat(PreferenceKeys.BestAccuracy(CytoidApplication.CurrentLevel, CytoidApplication.CurrentChartType, ranked), (float) tp);
		}

		var playCount =
			ZPlayerPrefs.GetInt(PreferenceKeys.PlayCount(CytoidApplication.CurrentLevel, CytoidApplication.CurrentChartType), defaultValue: 0);
		
		ZPlayerPrefs.SetInt(PreferenceKeys.PlayCount(CytoidApplication.CurrentLevel, CytoidApplication.CurrentChartType), playCount + 1);
	}

	public IEnumerator AutoUpload()
	{
		uploadButton.interactable = false;
		yield return new WaitForSeconds(1);
		
		if (CytoidApplication.CurrentRankedModeData != null) PostRankingData();
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

	public void CloseUploadWindow()
	{
		
	}

	public void PostRankingData()
	{
		if (CytoidApplication.CurrentRankedModeData == null || !User.Exists())
		{
			Popup.Make(this, "ERROR: You haven't signed in.");
			return;
		}
		uploadButton.interactable = false;
		StartCoroutine(PostRankingDataCoroutine());
	}

	public IEnumerator PostRankingDataCoroutine()
	{
		
		if (IsUploading)
		{
			Popup.Make(this, "Already uploading.");
		}
		IsUploading = true;
		
        Debug.Log("Posting ranking data");
		
		Popup.Make(this, "Uploading play data...");

		yield return new WaitForSeconds(1);

        var request = new UnityWebRequest(CytoidApplication.Host + "/rank/post", "POST") {timeout = 10};

        var bodyRaw = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(CytoidApplication.CurrentRankedModeData));
		print("Body: " + JsonConvert.SerializeObject(CytoidApplication.CurrentRankedModeData));
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
 
        yield return request.Send();
        
		var response = request.responseCode;
		
        if (response != 200 || request.isNetworkError || request.isHttpError) {

	        switch (response)
	        {
		        case 400:
			        Popup.Make(this, "ERROR: " + "Invalid play data.");
			        break;
		        case 401:
			        Popup.Make(this, "ERROR: " + "You haven't signed in.");
			        User.reset();
			        break;
		        case 403:
			        Popup.Make(this, "ERROR: " + request.downloadHandler.text);
			        break;
			    default:
				    Popup.Make(this, "ERROR: " + request.error + ".");
				    break;   
	        }
	        
	        CloseUploadWindow();
            IsUploading = false;
	        request.Dispose();
	        uploadButton.interactable = true;
            yield break;
        }
        
        Popup.Make(this, "Uploaded play data.");
		
		CloseUploadWindow();
		IsUploading = false;
		request.Dispose();
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
		PostRankingData();
	}
	
	void ShareScreenshot(Texture2D tex)
	{
		VSSHARE.DOShareScreenshot(VSSHARE.self.customShareText, ShareType.Native);
	}
	
}
