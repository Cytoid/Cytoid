using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Ink.Runtime;
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
    public RectTransform choicesRoot;
    public SoftButton choiceButtonPrefab;
    
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

    public static async void Show(Story story)
    {
        var instance = Instance;
        if (instance.IsShown) await UniTask.WaitUntil(() => !instance.IsShown);
        await instance.Enter();

        var spriteSets = new Dictionary<string, DialogueSpriteSet>();

        story.globalTags.FindAll(it => it.Trim().StartsWith("SpriteSet:"))
            .Select(it => it.Substring(it.IndexOf(':') + 1).Trim())
            .Select(DialogueSpriteSet.Parse)
            .ForEach(it => spriteSets[it.Id] = it);
        
        await spriteSets.Values.Select(it => it.Initialize());

        Sprite currentSprite = null;
        var currentSpeaker = "";
        var currentPosition = DialogueBoxPosition.Bottom;

        Dialogue lastDialogue = null;
        DialogueBox lastDialogueBox = null;
        while (story.canContinue)
        {
            var message = story.Continue();
            var tags = story.currentTags;

            var setSprite = TagValue(tags, "Sprite");
            if (setSprite != null)
            {
                if (setSprite == "null")
                {
                    currentSprite = null;
                }
                else
                {
                    setSprite.Split('/', out var id, out var state);

                    if (!spriteSets.ContainsKey(id)) throw new ArgumentOutOfRangeException();
                    currentSprite = spriteSets[id].States[state].Sprite;
                }
            }

            var setSpeaker = TagValue(tags, "Speaker");
            if (setSpeaker != null)
            {
                currentSpeaker = setSpeaker == "null" ? null : setSpeaker;
            }

            var setPosition = TagValue(tags, "Position");
            if (setPosition != null)
            {
                currentPosition = (DialogueBoxPosition) Enum.Parse(typeof(DialogueBoxPosition), setPosition);
            }
            
            var dialogue = new Dialogue
            {
                Message = message,
                SpeakerName = currentSpeaker,
                Sprite = currentSprite,
                Position = currentPosition,
                HasChoices = story.currentChoices.Count > 0
            };

            DialogueBox dialogueBox;
            if (dialogue.Position == DialogueBoxPosition.Top) dialogueBox = instance.topDialogueBox;
            else if (dialogue.Sprite != null) dialogueBox = instance.bottomFullDialogueBox;
            else dialogueBox = instance.bottomDialogueBox;

            if (lastDialogue != null && (lastDialogueBox != dialogueBox || lastDialogue.SpeakerName != dialogue.SpeakerName))
            {
                await lastDialogueBox.SetDisplayed(false);
                dialogueBox.messageBox.SetLocalScale(1f);
            }
            await dialogueBox.SetDisplayed(true);
            lastDialogue = dialogue;
            lastDialogueBox = dialogueBox;
            
            instance.detectionArea.onPointerClick.SetListener(_ =>
            {
                dialogueBox.WillFastForwardDialogue = true;
            });
            await dialogueBox.ShowDialogue(dialogue);
            
            if (story.currentChoices.Count > 0)
            {
                var proceed = false;
                var buttons = new List<SoftButton>();
                for (var index = 0; index < story.currentChoices.Count; index++)
                {
                    var choice = story.currentChoices[index];
                    var choiceButton = Instantiate(instance.choiceButtonPrefab, instance.choicesRoot);
                    var closureIndex = index;
                    choiceButton.onPointerClick.SetListener(_ =>
                    {
                        if (proceed) return;
                        story.ChooseChoiceIndex(closureIndex);
                        proceed = true;
                    });
                    choiceButton.SetText(choice.text);
                    buttons.Add(choiceButton);
                }

                LayoutFixer.Fix(instance.choicesRoot);
                await UniTask.DelayFrame(4);

                foreach (var button in buttons)
                {
                    button.transitionElement.Enter();
                    await UniTask.Delay(TimeSpan.FromSeconds(0.2f));
                }
                
                await UniTask.WaitUntil(() => proceed);

                buttons.ForEach(it => Destroy(it.gameObject));
            }
            else
            {
                var proceed = false;
                instance.detectionArea.onPointerDown.SetListener(_ => { dialogueBox.messageBox.DOScale(0.97f, 0.2f); });
                instance.detectionArea.onPointerUp.SetListener(_ =>
                {
                    dialogueBox.messageBox.DOScale(1f, 0.2f);
                    proceed = true;
                });
                await UniTask.WaitUntil(() => proceed);
                instance.detectionArea.onPointerDown.RemoveAllListeners();
                instance.detectionArea.onPointerUp.RemoveAllListeners();
            }
        }
        if (lastDialogueBox != null) lastDialogueBox.SetDisplayed(false);
        await instance.Leave();

        spriteSets.Values.ForEach(it => it.Dispose());

        string TagValue(List<string> tags, string tag)
        {
            return tags.Find(it => it.Trim().StartsWith(tag + ":"))?.Let(it => it.Substring(it.IndexOf(':') + 1).Trim());
        }
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
                var intro = Resources.Load<TextAsset>("Stories/Intro");
                var story = new Story(intro.text);
                DialogueOverlay.Show(story);
            }
        }
    }
}
#endif