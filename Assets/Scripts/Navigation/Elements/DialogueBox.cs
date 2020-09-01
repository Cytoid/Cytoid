using System;
using System.Collections.Generic;
using DG.Tweening;
using Cysharp.Threading.Tasks;
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
            transitionElements.ForEach(it => it.Enter(false));
        }
        else
        {
            transitionElements.ForEach(it => it.Leave(false));
            await UniTask.Delay(TimeSpan.FromSeconds(0.4f));
            if (token != displayToken) return;
            caretImage.DOKill();
            caretImage.SetAlpha(0);
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
            if (dialogue.AnimatorController != null)
            {
                chibiDisplay.SetAnimation(dialogue.AnimatorController, dialogue.AnimationName);
            }
            else
            {
                chibiDisplay.SetSprite(dialogue.Sprite);
            }
        }
        if (speakerNameText != null)
        {
            speakerNameText.text = dialogue.SpeakerName;
        }

        caretImage.DOKill();
        caretImage.DOFade(0, 0.4f);
        WillFastForwardDialogue = false;
        dialogue.Message = dialogue.Message.Replace('/', '\n');
        var baseDelay = 2;
        var isBold = false;
        var baseText = "";
        foreach (var c in dialogue.Message)
        {
            var delay = baseDelay;
            if (c == '^')
            {
                if (baseDelay > 0) delay = 10;
            }
            else if (c == '†')
            {
                isBold = !isBold;
                baseText += isBold ? "<b>" : "</b>";
            }
            else
            {
                baseText += c;
            }

            messageText.text = baseText + (isBold ? "</b>" : "");
            
            if (delay > 0) await UniTask.DelayFrame(delay);

            if (WillFastForwardDialogue)
            {
                baseDelay = 0;
            }
        }
        WillFastForwardDialogue = false;
        if (!dialogue.HasChoices && !dialogue.IsBlocked)
        {
            caretImage.DOFade(1, 0.4f);
            
            tweenSequences.ForEach(it => it.Kill());
            tweenSequences.Clear();
            caretImage.rectTransform.SetAnchoredY(15.0f);
            tweenSequences.Add(DOTween.Sequence()
                .Append(caretImage.rectTransform.DOAnchorPosY(39.0f, 0.6f))
                .Append(caretImage.rectTransform.DOAnchorPosY(15.0f, 0.6f))
                .SetLoops(-1));
        }
    }
    
}

public class Dialogue
{
    public DialogueBoxPosition Position { get; set; }
    public string Message { get; set; }
    public string SpeakerName { get; set; }
    public Sprite Sprite { get; set; }
    public RuntimeAnimatorController AnimatorController { get; set; }
    public string AnimationName { get; set; }
    public bool HasChoices { get; set; }
    public bool IsBlocked { get; set; }
}

public enum DialogueBoxPosition
{
    Top,
    Bottom
}

public class DialogueSpriteSet
{

    public static DialogueSpriteSet Parse(string name)
    {
        /*switch (name)
        {
            case "Tira":
                return new DialogueSpriteSet
                {
                    Id = "Tira",
                    States = new Dictionary<string, State>
                    {
                        {"Default", new State {SpriteAddress = "Stories/Characters/Tira/Default"}}
                    }
                };
        }*/
        throw new InvalidOperationException(name);
    }
    
    public string Id { get; set; }

    public Dictionary<string, State> States { get; set; } = new Dictionary<string, State>();

    public class State
    {
        public string SpriteAddress { get; set; }
        public Sprite Sprite { get; set; }
    }

    public async UniTask Initialize()
    {
        foreach (var kv in States)
        {
            var state = kv.Value;
            if (state.SpriteAddress != null)
            {
                state.Sprite = (Sprite) await Resources.LoadAsync<Sprite>(state.SpriteAddress);
            }
        }
    }

    public void Dispose()
    {
        States.ForEach(it =>
        {
            if (it.Value.Sprite != null) Resources.UnloadAsset(it.Value.Sprite);
        });
    }
}

public class DialogueAnimationSet
{
    public static DialogueAnimationSet Parse(string name)
    {
        switch (name)
        {
            case "Sayaka":
                return new DialogueAnimationSet
                {
                    Id = "Sayaka",
                    ControllerAddress = "Stories/Characters/Sayaka/Controller"
                };
            case "Kaede":
                return new DialogueAnimationSet
                {
                    Id = "Kaede",
                    ControllerAddress = "Stories/Characters/Kaede/Controller"
                };
        }
        throw new InvalidOperationException(name);
    }
    
    public string Id { get; set; }

    public string ControllerAddress { get; set; }
    
    public RuntimeAnimatorController Controller { get; set; }

    public async UniTask Initialize()
    {
        Controller = (RuntimeAnimatorController) await Resources.LoadAsync<RuntimeAnimatorController>(ControllerAddress);
    }

    public void Dispose()
    {
        if (Controller != null) Resources.UnloadAsset(Controller);
    }
}