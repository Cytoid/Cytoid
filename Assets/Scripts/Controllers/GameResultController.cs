using UnityEngine;
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
	[SerializeField] private Text tpText;
	[SerializeField] private Text comboText;
	[SerializeField] private Text infoText;
	[SerializeField] private Button nextButton;
	[SerializeField] private Button retryButton;
	
	private void Start()
	{	
		
		BackgroundCanvasHelper.SetupBackgroundCanvas(gameObject.scene);
		
		// HIGHLIGHT
		Resources.UnloadUnusedAssets();
		// HIGHLIGHT

		var score = CytoidApplication.LastPlayResult.Score;
		var tp = CytoidApplication.LastPlayResult.Tp;

		titleText.text = CytoidApplication.CurrentLevel.title;
		scoreText.text = Mathf.CeilToInt(score).ToString("D6");
		tpText.text = tp.ToString("0.##") + "% Accuracy";
		comboText.text = CytoidApplication.LastPlayResult.MaxCombo + " Max. Combo";

		var result = CytoidApplication.LastPlayResult;
		var info = "";
		info += result.PerfectCount + " Perfect   ";
		info += result.ExcellentCount + " Excellent   ";
		info += result.GoodCount + " Good   ";
		info += result.BadCount + " Bad   ";
		info += result.MissCount + " Miss";

		infoText.text = info;
		
		DisplayDifficultyView.Instance.SetDifficulty(CytoidApplication.CurrentChartType, CytoidApplication.CurrentLevel.GetDifficulty(CytoidApplication.CurrentChartType));
		
		// Save stats
		var oldScore = ZPlayerPrefs.GetFloat(PreferenceKeys.BestScore(CytoidApplication.CurrentLevel,
			CytoidApplication.CurrentChartType));
		var oldTp = ZPlayerPrefs.GetFloat(PreferenceKeys.BestAccuracy(CytoidApplication.CurrentLevel,
			CytoidApplication.CurrentChartType));

		if (score > oldScore)
		{
			ZPlayerPrefs.SetFloat(PreferenceKeys.BestScore(CytoidApplication.CurrentLevel, CytoidApplication.CurrentChartType), score);
		}
		if (tp > oldTp)
		{
			ZPlayerPrefs.SetFloat(PreferenceKeys.BestAccuracy(CytoidApplication.CurrentLevel, CytoidApplication.CurrentChartType), tp);
		}

		var playCount =
			ZPlayerPrefs.GetInt(PreferenceKeys.PlayCount(CytoidApplication.CurrentLevel, CytoidApplication.CurrentChartType), defaultValue: 0);
		
		ZPlayerPrefs.SetInt(PreferenceKeys.PlayCount(CytoidApplication.CurrentLevel, CytoidApplication.CurrentChartType), playCount + 1);
		
		if (!CytoidApplication.UseDoozyUI)
		{
			nextButton.onClick.AddListener(() =>
			{
				action = Action.Next;
				DoAction();
			});
			retryButton.onClick.AddListener(() =>
			{
				action = Action.Retry;
				DoAction();
			});
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
				SceneManager.LoadScene("Game");
				break;
			case Action.Next:
				SceneManager.LoadScene("LevelSelection");
				break;
		}
	}
	
}
