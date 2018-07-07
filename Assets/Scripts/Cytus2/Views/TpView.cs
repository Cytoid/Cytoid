using System;
using Cytus2.Controllers;
using UnityEngine;
using UnityEngine.UI;

namespace Cytus2.Views
{
    public class TpView : MonoBehaviour
    {
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
            text.text = game.PlayData == null || game.PlayData.NoteCleared == 0 || Math.Abs(game.PlayData.Tp - 100) < 0.0001 ? "100% accuracy" : game.PlayData.Tp.ToString("0.##") + "% accuracy";
        }
    }
}