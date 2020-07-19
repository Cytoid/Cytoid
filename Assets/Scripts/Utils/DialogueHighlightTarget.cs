using System;
using System.Linq;
using UnityEngine;

public class DialogueHighlightTarget : MonoBehaviour
{
    [GetComponent] public Canvas canvas;
    [GetComponent] public CanvasGroup canvasGroup;
    public string id;

    private bool highlighted;
    private bool defaultOverrideSorting;
    private int defaultSortingOrder;
    private bool defaultInteractable;
    private bool defaultBlocksRaycasts;
    private bool defaultIgnoreParentGroups;

    public bool Highlighted
    {
        get => highlighted;
        set
        {
            if (highlighted == value) return;
            highlighted = value;
            UpdateHighlight();
        }
    }

    private void UpdateHighlight()
    {
        if (highlighted)
        {
            defaultOverrideSorting = canvas.overrideSorting;
            defaultSortingOrder = canvas.sortingOrder;
            defaultInteractable = canvasGroup.interactable;
            defaultBlocksRaycasts = canvasGroup.blocksRaycasts;
            defaultIgnoreParentGroups = canvasGroup.ignoreParentGroups;
            
            canvas.overrideSorting = true;
            canvas.sortingOrder = 10000;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.ignoreParentGroups = true;
        }
        else
        {
            canvas.overrideSorting = defaultOverrideSorting;
            canvas.sortingOrder = defaultSortingOrder;
            canvasGroup.interactable = defaultInteractable;
            canvasGroup.blocksRaycasts = defaultBlocksRaycasts;
            canvasGroup.ignoreParentGroups = defaultIgnoreParentGroups;
        }
    }

    public static DialogueHighlightTarget Find(string id)
    {
        return FindObjectsOfType<DialogueHighlightTarget>().ToList().Find(it => it.id == id);
    }

}