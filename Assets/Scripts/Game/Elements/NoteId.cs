using System;
using TMPro;
using UnityEngine;

public class NoteId : MonoBehaviour
{
    
    public bool Visible
    {
        get => visible;
        set
        {
            visible = value;
            text.color = text.color.WithAlpha(visible ? 1 : 0);
        }
    }
    
    public TextMeshPro text;
    private bool visible;
    
    public void SetModel(ChartModel.Note note)
    {
        text.text = note.id.ToString();
        switch (note.type)
        {
            case (int) NoteType.DragHead:
            case (int) NoteType.CDragHead:
                transform.localScale *= 0.8f;
                text.color = Color.black;
                break;
            case (int) NoteType.DragChild:
            case (int) NoteType.CDragChild:
                transform.localScale *= 0.6f;
                text.color = Color.black;
                break;
            case (int) NoteType.Flick:
                text.color = Color.black;
                break;
        }
    }

}