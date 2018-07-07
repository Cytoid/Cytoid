using Cytus2.Controllers;
using UnityEngine;
using UnityEngine.UI;

namespace Cytus2.Views
{

    public class ScoreView : MonoBehaviour
    {

        private Text text;
        private string lastScore = "000000";
	
        private Game game;

        private void Awake()
        {
            text = GetComponent<Text>();
        }

        private void Start()
        {
            game = Game.Instance;
        }

        private void LateUpdate()
        {
            if (!game.IsPlaying)
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
            var score = Mathf.CeilToInt((float) game.PlayData.Score).ToString("D6");
            lastScore = score;
            text.text = game.PlayData == null ? "000000" : lastScore;
        }
	
    }

}