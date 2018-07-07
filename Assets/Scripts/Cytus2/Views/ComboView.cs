using Cytus2.Controllers;
using UnityEngine;
using UnityEngine.UI;

namespace Cytus2.Views
{
    
    public class ComboView : MonoBehaviour {

        private Text text;
	
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
            if (!game.IsPlaying) return;
            text.text = game.PlayData == null ? "0 combo" : game.PlayData.Combo + " combo";
        }
	
    }

}