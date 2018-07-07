using System;
using UnityEngine;

public class ChainHeadView : MonoBehaviour
{

    public SpriteRenderer backRenderer;
    public SpriteRenderer fillRenderer;
    
    [HideInInspector] public GameController game;
    [HideInInspector] public OldScannerView scanner;
    [HideInInspector] public ChainNoteView previousNoteView;
    [HideInInspector] public ChainNoteView nextNoteView;

    private void Start()
    {
        scanner = OldScannerView.Instance;
        backRenderer.color = new Color(backRenderer.color.r, backRenderer.color.g, backRenderer.color.b, 0);
        fillRenderer.color = new Color(fillRenderer.color.r, fillRenderer.color.g, fillRenderer.color.b, 0);
        backRenderer.sortingOrder = nextNoteView.ringSpriteRenderer.sortingOrder + 1;
        fillRenderer.sortingOrder = backRenderer.sortingOrder + 1;
    }

    private bool freeze = false;

    public void Update()
    {
        if (Math.Abs(backRenderer.color.a - 1f) > 0.001)
        {
            var newColor = backRenderer.color;
            newColor.a = nextNoteView.ringSpriteRenderer.color.a * 1.5f;
            backRenderer.color = newColor;
            newColor = fillRenderer.color;
            newColor.a = nextNoteView.fillSpriteRenderer.color.a * 1.5f;
            fillRenderer.color = newColor;
        }
        if (freeze) return;
        var currentNoteView = nextNoteView;
        if (game.TimeElapsed > currentNoteView.note.time)
        {
            if (currentNoteView.connectedNoteView != null)
            {
                previousNoteView = currentNoteView;
                nextNoteView = currentNoteView.connectedNoteView;
                transform.position = currentNoteView.transform.position;
            }
            else
            {
                freeze = true;
            }
        }
        if (previousNoteView != null)
        {
            transform.position = Vector3.Lerp(previousNoteView.transform.position, nextNoteView.transform.position,
                (scanner.transform.position.y - previousNoteView.transform.position.y) / (nextNoteView.transform.position.y - previousNoteView.transform.position.y + 0.00001f));
        }
    }
    
}