using UnityEngine;
using UnityEngine.UI;

public class ScoreView : MonoBehaviour
{

	private Text text;
	private string lastScore;
	
	private GameController game;
	private float minAlpha;

	private void Awake()
	{
		text = GetComponent<Text>();
		minAlpha = text.color.a;
	}

	private void Start()
	{
		game = GameController.Instance;
	}

	private void Update()
	{
		/*var a = text.color.a - 2f * Time.deltaTime;
		if (a < minAlpha) a = minAlpha;
		text.ChangeColor(a:a);*/
	}

	private void FixedUpdate()
	{
		if (game.IsPaused)
		{
			if (game.UnpauseCountdown != -1)
			{
				text.text = "In " + game.UnpauseCountdown + "...";
			}
			else
			{
				text.text = lastScore;
			}
			return;
		}
		var score = Mathf.CeilToInt(game.PlayData.Score).ToString("D6");
		if (score != lastScore)
		{
			// text.ChangeColor(a: 1);
		}
		lastScore = score;
		text.text = game.PlayData == null ? "000000" : lastScore;
	}
	
}
