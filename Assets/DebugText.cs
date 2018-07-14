using System.Collections;
using System.Collections.Generic;
using Cytus2.Controllers;
using UnityEngine;
using UnityEngine.UI;

public class DebugText : MonoBehaviour
{
    private Text text;
    private Game game;
    private int noteCount;

    // Use this for initialization
    void Start()
    {
        text = GetComponent<Text>();
        game = Game.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        if (game.Play.NoteRankings == null)
        {
            return;
        }

        if (noteCount == 0)
        {
            noteCount = game.Chart.Root.note_list.Count;
        }

        text.text = game.Play.NoteCleared + "/" + noteCount + " | ";

        for (var i = 0; i < noteCount; i++)
        {
            if (!game.Play.NoteRankings.ContainsKey(i))
            {
                text.text += "!" + i + " ";
                continue;
            }

            if (game.Play.NoteRankings[i] == NoteGrading.Undetermined)
                text.text += i + " ";
        }
    }
}