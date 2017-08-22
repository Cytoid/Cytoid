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
		
		scoreText.text = Mathf.CeilToInt(CytoidApplication.LastPlayResult.Score).ToString("D6");
		tpText.text = CytoidApplication.LastPlayResult.Tp.ToString("0.##") + "% Accuracy";
		comboText.text = CytoidApplication.LastPlayResult.MaxCombo + " Max. Combo";
		infoText.text =
			"P:" + CytoidApplication.LastPlayResult.PerfectCount + "  " +
			"E:" + CytoidApplication.LastPlayResult.ExcellentCount + "  " +
			"G:" + CytoidApplication.LastPlayResult.GoodCount + "  " +
			"B:" + CytoidApplication.LastPlayResult.BadCount + "  " +
			"M:" + CytoidApplication.LastPlayResult.MissCount;
		
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
