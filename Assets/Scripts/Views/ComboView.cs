using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ComboView : MonoBehaviour {

	private Text text;
	
	private GameController game;

	private void Awake()
	{
		text = GetComponent<Text>();
	}

	private void Start()
	{
		game = GameController.Instance;
	}

	private void FixedUpdate()
	{
		if (game.IsPaused) return;
		text.text = game.PlayData == null ? "0 combo" : game.PlayData.Combo + " combo";
	}
	
}
