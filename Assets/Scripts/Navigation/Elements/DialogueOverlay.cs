using System;
using System.Collections.Generic;
using DG.Tweening;
using Newtonsoft.Json;
using UniRx.Async;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class DialogueOverlay : SingletonMonoBehavior<DialogueOverlay>
{
    [GetComponent] public Canvas canvas;
    [GetComponent] public CanvasGroup canvasGroup;
    public InteractableMonoBehavior detectionArea;
    public GameObject parent;
    public DialogueBox topDialogueBox;
    public DialogueBox bottomDialogueBox;
    public DialogueBox bottomFullDialogueBox;
    
    public bool IsShown { get; private set; }

    private void Start()
    {
        canvas.enabled = false;
        canvasGroup.enabled = false;
        canvas.overrideSorting = true;
        canvas.sortingOrder = NavigationSortingOrder.DialogueOverlay;
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        parent.SetActive(false);
        topDialogueBox.SetDisplayed(false);
        bottomDialogueBox.SetDisplayed(false);
        bottomFullDialogueBox.SetDisplayed(false);
    }

    private async UniTask Enter()
    {
        IsShown = true;
        canvas.enabled = true;
        canvas.overrideSorting = true;
        canvas.sortingOrder = NavigationSortingOrder.DialogueOverlay;
        canvasGroup.enabled = true;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        canvasGroup.DOKill();
        canvasGroup.DOFade(1, 0.4f).SetEase(Ease.OutCubic);
        parent.SetActive(true);   
        Context.SetMajorCanvasBlockRaycasts(false);
        await UniTask.Delay(TimeSpan.FromSeconds(0.4f));
    }
    
    private async UniTask Leave()
    {
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.DOKill();
        canvasGroup.DOFade(0, 0.4f).SetEase(Ease.OutCubic);
        Context.SetMajorCanvasBlockRaycasts(true);
        await UniTask.Delay(TimeSpan.FromSeconds(0.4f));
        canvas.enabled = false;
        topDialogueBox.SetDisplayed(false);
        bottomDialogueBox.SetDisplayed(false);
        bottomFullDialogueBox.SetDisplayed(false);
        canvasGroup.enabled = false;
        parent.SetActive(false);
        IsShown = false;
    }

    public static async void Show(List<Dialogue> dialogues)
    {
        var instance = Instance;
        if (instance.IsShown) await UniTask.WaitUntil(() => !instance.IsShown);
        await instance.Enter();
        DialogueBox lastDialogueBox = null;
        foreach (var dialogue in dialogues)
        {
            if (dialogue.Position == DialogueBoxPosition.Top && (dialogue.ChibiSprite != null || dialogue.SpeakerName != null)) throw new InvalidOperationException();
            DialogueBox dialogueBox;
            if (dialogue.Position == DialogueBoxPosition.Top) dialogueBox = instance.topDialogueBox;
            else if (dialogue.ChibiSprite != null) dialogueBox = instance.bottomFullDialogueBox;
            else dialogueBox = instance.bottomDialogueBox;

            if (lastDialogueBox != null && lastDialogueBox != dialogueBox)
            {
                await lastDialogueBox.SetDisplayed(false);
                dialogueBox.messageBox.SetLocalScale(1f);
            }
            await dialogueBox.SetDisplayed(true);
            lastDialogueBox = dialogueBox;
            
            instance.detectionArea.onPointerClick.SetListener(_ =>
            {
                dialogueBox.WillFastForwardDialogue = true;
            });
            await dialogueBox.ShowDialogue(dialogue);
            var proceed = false;
            instance.detectionArea.onPointerDown.SetListener(_ =>
            {
                dialogueBox.messageBox.DOScale(0.97f, 0.2f);
            });
            instance.detectionArea.onPointerUp.SetListener(_ =>
            {
                dialogueBox.messageBox.DOScale(1f, 0.2f);
                proceed = true;
            });
            await UniTask.WaitUntil(() => proceed);
        }
        if (lastDialogueBox != null) lastDialogueBox.SetDisplayed(false);
        await instance.Leave();
    }
    
}


#if UNITY_EDITOR

[CustomEditor(typeof(DialogueOverlay))]
public class DialogueOverlayEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (Application.isPlaying)
        {
            if (GUILayout.Button("Test"))
            {
                DialogueOverlay.Show(new List<Dialogue>
                {
                    new Dialogue
                    {
                        SpeakerName = "Tira",
                        ChibiSprite = "tira",
                        Message = "欢迎来到 Cytoid！"
                    },
                    new Dialogue
                    {
                        Position = DialogueBoxPosition.Top,
                        Message = "THIS IS A TEST THIS IS A TEST THIS IS A TEST THIS IS A TEST THIS IS A TEST THIS IS A TEST THIS IS A TEST THIS IS A TEST THIS IS A TEST THIS IS A TEST THIS IS A TEST THIS IS A TEST THIS IS A TEST "
                    },
                    new Dialogue
                    {
                        SpeakerName = "Tira",
                        ChibiSprite = "tira",
                        Message = "啦啦啦啦啦！"
                    },
                    new Dialogue
                    {
                        SpeakerName = "Tira",
                        ChibiSprite = "tira",
                        Message = "耶！！！！"
                    },
                    new Dialogue
                    {
                        Message = "对话第一条"
                    },
                    new Dialogue
                    {
                        Message = "对话第二条"
                    },
                    new Dialogue
                    {
                        Message = "对话第三条"
                    }
                });
            }
        }
    }
}
#endif