using System;
using System.Collections.Generic;
using DG.Tweening;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class DialogueBox : MonoBehaviour
{
    [GetComponent] public Canvas canvas;
    public List<TransitionElement> transitionElements;
    public ChibiDisplay chibiDisplay;
    public RectTransform messageBox;
    public Text messageText;
    public Text speakerNameText;
    public Image caretImage;

    public bool WillFastForwardDialogue { get; set; }
    
    private DateTimeOffset displayToken;
    private bool displayed;
    private List<Sequence> tweenSequences = new List<Sequence>();

    private void Awake()
    {
        caretImage.rectTransform.SetAnchoredY(15.0f);
    }

    public async UniTask SetDisplayed(bool displayed)
    {
        if (displayed == this.displayed) return;
        this.displayed = displayed;
        var token = displayToken = DateTimeOffset.Now;
        if (displayed)
        {
            canvas.enabled = true;
            canvas.overrideSorting = true;
            canvas.sortingOrder = NavigationSortingOrder.DialogueBox;
            caretImage.DOKill();
            caretImage.SetAlpha(0);
            caretImage.rectTransform.DOKill();
            caretImage.rectTransform.SetAnchoredY(15.0f);
            transitionElements.ForEach(it => it.Enter(false));
            tweenSequences.ForEach(it => it.Kill());
            tweenSequences.Clear();
        }
        else
        {
            transitionElements.ForEach(it => it.Leave(false));
            await UniTask.Delay(TimeSpan.FromSeconds(0.4f));
            if (token != displayToken) return;
            caretImage.DOKill();
            caretImage.SetAlpha(0);
            caretImage.rectTransform.DOKill();
            caretImage.rectTransform.SetAnchoredY(15.0f);
            tweenSequences.ForEach(it => it.Kill());
            tweenSequences.Clear();
            messageText.text = "";
            if (chibiDisplay != null)
            {
                chibiDisplay.SetSprite(null);
            }
            if (speakerNameText != null)
            {
                speakerNameText.text = "";
            }
            canvas.enabled = false;
        }
    }

    public async UniTask ShowDialogue(Dialogue dialogue)
    {
        messageText.text = "";
        if (chibiDisplay != null)
        {
            chibiDisplay.SetSprite(dialogue.ChibiSprite);
        }
        if (speakerNameText != null)
        {
            speakerNameText.text = dialogue.SpeakerName;
        }
        caretImage.rectTransform.DOKill();
        caretImage.DOFade(0, 0.4f);
        WillFastForwardDialogue = false;
        foreach (var c in dialogue.Message)
        {
            messageText.text += c;
            await UniTask.DelayFrame(2);
            if (WillFastForwardDialogue) break;
        }
        WillFastForwardDialogue = false;
        messageText.text = dialogue.Message;
        caretImage.DOFade(1, 0.4f);
        tweenSequences.ForEach(it => it.Kill());
        tweenSequences.Clear();
        tweenSequences.Add(DOTween.Sequence()
            .Append(caretImage.rectTransform.DOAnchorPosY(39.0f, 0.6f))
            .Append(caretImage.rectTransform.DOAnchorPosY(15.0f, 0.6f))
            .SetLoops(-1));
    }
    
}

public class Dialogue
{
    public DialogueBoxPosition Position { get; set; } = DialogueBoxPosition.Bottom;
    public string Message { get; set; }
    public string SpeakerName { get; set; }
    public string ChibiSprite { get; set; }
}

public enum DialogueBoxPosition
{
    Top,
    Bottom
}