using TMPro;
using UnityEngine;

public class NoteId : MonoBehaviour
{
    public TextMeshPro text;

    public void SetModel(ChartModel.Note note)
    {
        text.text = note.id.ToString();
        switch (note.type)
        {
            case (int) NoteType.DragHead:
                transform.localScale *= 0.8f;
                text.color = Color.black;
                break;
            case (int) NoteType.DragChild:
                transform.localScale *= 0.6f;
                text.color = Color.black;
                break;
            case (int) NoteType.Flick:
                text.color = Color.black;
                break;
        }
    }
}