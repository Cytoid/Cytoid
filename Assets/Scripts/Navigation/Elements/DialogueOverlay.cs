using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Ink.Runtime;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class DialogueOverlay : SingletonMonoBehavior<DialogueOverlay>
{
    public static Badge CurrentBadge;
    
    [GetComponent] public Canvas canvas;
    [GetComponent] public CanvasGroup canvasGroup;
    public Image backdropImage;
    public InteractableMonoBehavior detectionArea;
    public GameObject parent;
    public DialogueBox topDialogueBox;
    public DialogueBox bottomDialogueBox;
    public DialogueBox bottomFullDialogueBox;
    public RectTransform choicesRoot;
    public SoftButton choiceButtonPrefab;
    public DialogueImage image;
    
    public bool IsActive { get; private set; }

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
        backdropImage.SetAlpha(0.7f);
    }

    private async UniTask Enter()
    {
        backdropImage.DOKill();
        backdropImage.SetAlpha(0.7f);
        IsActive = true;
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
        IsActive = false;
    }

    public static bool IsShown() => Instance.IsActive;

    public static async UniTask Show(Story story)
    {
        var instance = Instance;
        if (instance.IsActive) await UniTask.WaitUntil(() => !instance.IsActive);
        await instance.Enter();

        var spriteSets = new Dictionary<string, DialogueSpriteSet>();
        var animationSets = new Dictionary<string, DialogueAnimationSet>();

        if (story.globalTags != null)
        {
            story.globalTags.FindAll(it => it.Trim().StartsWith("SpriteSet:"))
                .Select(it => it.Substring(it.IndexOf(':') + 1).Trim())
                .Select(DialogueSpriteSet.Parse)
                .ForEach(it => spriteSets[it.Id] = it);
            story.globalTags.FindAll(it => it.Trim().StartsWith("AnimationSet:"))
                .Select(it => it.Substring(it.IndexOf(':') + 1).Trim())
                .Select(DialogueAnimationSet.Parse)
                .ForEach(it => animationSets[it.Id] = it);
        }

        await spriteSets.Values.Select(it => it.Initialize());
        await animationSets.Values.Select(it => it.Initialize());

        Sprite currentImageSprite = null;
        string currentImageSpritePath = null;
        Sprite currentSprite = null;
        string currentAnimation = null;
        var currentSpeaker = "";
        var currentPosition = DialogueBoxPosition.Bottom;

        var shouldSetImageSprite = false;
        Dialogue lastDialogue = null;
        DialogueBox lastDialogueBox = null;
        DialogueHighlightTarget lastHighlightTarget = null;

        while (story.canContinue)
        {
            var message = ReplacePlaceholders(story.Continue());
            var duration = 0f;
            var tags = story.currentTags;

            var setDuration = TagValue(tags, "Duration");
            if (setDuration != null)
            {
                duration = float.Parse(setDuration);
            }
            
            var setOverlayOpacity = TagValue(tags, "OverlayOpacity");
            if (setOverlayOpacity != null)
            {
                setOverlayOpacity.Split('/', out var targetOpacity, out var fadeDuration, out var fadeDelay);
                SetOverlayOpacity().Forget();
                async UniTaskVoid SetOverlayOpacity()
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(float.Parse(fadeDelay)));
                    instance.backdropImage.DOFade(float.Parse(targetOpacity), float.Parse(fadeDuration));
                }
            }
            
            var setHighlight = TagValue(tags, "Highlight");
            if (setHighlight != null)
            {
                lastHighlightTarget = await DialogueHighlightTarget.Find(setHighlight);
                if (lastHighlightTarget != null)
                {
                    lastHighlightTarget.Highlighted = true;
                }
            }

            var waitForHighlightOnClick = FlagValue(tags, "WaitForHighlightOnClick");
            if (waitForHighlightOnClick && lastHighlightTarget == null)
            {
                waitForHighlightOnClick = false;
            }
            
            var setImage = TagValue(tags, "Image");
            string imageWidth = null, imageHeight = null, imageRadius = null;
            if (setImage != null)
            {
                if (setImage == "null")
                {
                    currentImageSprite = null;
                }
                else
                {
                    setImage.Split('/', out imageWidth, out imageHeight, out imageRadius, out var imageUrl);

                    imageUrl = ReplacePlaceholders(imageUrl);
                    
                    SpinnerOverlay.Show();
                    currentImageSprite = await Context.AssetMemory.LoadAsset<Sprite>(imageUrl, AssetTag.DialogueImage);
                    currentImageSpritePath = imageUrl;
                    SpinnerOverlay.Hide();

                    shouldSetImageSprite = true;
                }
            }

            var setSprite = TagValue(tags, "Sprite");
            if (setSprite != null)
            {
                if (setSprite == "null")
                {
                    currentSprite = null;
                    DisposeImageSprite();
                }
                else
                {
                    setSprite.Split('/', out var id, out var state);

                    if (!spriteSets.ContainsKey(id)) throw new ArgumentOutOfRangeException();
                    currentSprite = spriteSets[id].States[state].Sprite;
                    currentAnimation = null;
                }
            }
           
            var setAnimation = TagValue(tags, "Animation");
            if (setAnimation != null)
            {
                if (setAnimation == "null")
                {
                    currentAnimation = null;
                }
                else
                {
                    currentAnimation = setAnimation;
                    currentSprite = null;
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
                HasChoices = story.currentChoices.Count > 0,
                IsBlocked = waitForHighlightOnClick || duration > 0
            };
            
            // Lookup animation
            if (currentAnimation != null)
            {
                currentAnimation.Split('/', out var id, out var animationName);
                
                if (!animationSets.ContainsKey(id)) throw new ArgumentOutOfRangeException();
                dialogue.AnimatorController = animationSets[id].Controller;
                dialogue.AnimationName = animationName;
            }

            DialogueBox dialogueBox;
            if (dialogue.Position == DialogueBoxPosition.Top) dialogueBox = instance.topDialogueBox;
            else if (dialogue.Sprite != null || dialogue.AnimatorController != null) dialogueBox = instance.bottomFullDialogueBox;
            else dialogueBox = instance.bottomDialogueBox;

            if (lastDialogue != null && (lastDialogueBox != dialogueBox || lastDialogue.SpeakerName != dialogue.SpeakerName))
            {
                await lastDialogueBox.SetDisplayed(false);
                dialogueBox.messageBox.SetLocalScale(1f);
            }
            
            // Display image
            if (currentImageSprite != null)
            {
                if (shouldSetImageSprite)
                {
                    instance.image.SetData(currentImageSprite, int.Parse(imageWidth), int.Parse(imageHeight), int.Parse(imageRadius));
                    await UniTask.Delay(TimeSpan.FromSeconds(1.5f));
                    shouldSetImageSprite = false;
                }
            }
            else
            {
                instance.image.Clear();
            }

            if (message.IsNullOrEmptyTrimmed())
            {
                await dialogueBox.SetDisplayed(false);
            }
            else
            {
                await dialogueBox.SetDisplayed(true);
                lastDialogue = dialogue;
                lastDialogueBox = dialogueBox;

                instance.detectionArea.onPointerClick.SetListener(_ => { dialogueBox.WillFastForwardDialogue = true; });
                await dialogueBox.ShowDialogue(dialogue);
            }

            if (waitForHighlightOnClick)
            {
                await lastHighlightTarget.WaitForOnClick();
            }
            else if (story.currentChoices.Count > 0)
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
                    choiceButton.Label = choice.text;
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
                instance.detectionArea.onPointerDown.SetListener(_ => { dialogueBox.messageBox.DOScale(0.95f, 0.2f); });
                instance.detectionArea.onPointerUp.SetListener(_ =>
                {
                    dialogueBox.messageBox.DOScale(1f, 0.2f);
                    proceed = true;
                });
                if (duration > 0)
                {
                    await UniTask.WhenAny(UniTask.WaitUntil(() => proceed),
                        UniTask.Delay(TimeSpan.FromSeconds(duration)));
                }
                else
                {
                    await UniTask.WaitUntil(() => proceed);
                }
                instance.detectionArea.onPointerDown.RemoveAllListeners();
                instance.detectionArea.onPointerUp.RemoveAllListeners();
            }

            if (lastHighlightTarget != null)
            {
                lastHighlightTarget.Highlighted = false;
                lastHighlightTarget = null;
            }
        }
        if (lastDialogueBox != null) lastDialogueBox.SetDisplayed(false);
        
        instance.image.Clear();
        await instance.Leave();

        DisposeImageSprite();
        spriteSets.Values.ForEach(it => it.Dispose());
        animationSets.Values.ForEach(it => it.Dispose());

        string TagValue(List<string> tags, string tag)
        {
            return tags.Find(it => it.Trim().StartsWith(tag + ":"))?.Let(it => it.Substring(it.IndexOf(':') + 1).Trim());
        }
        
        bool FlagValue(List<string> tags, string tag)
        {
            return tags.Any(it => it.Trim() == tag);
        }

        void DisposeImageSprite()
        {
            if (currentImageSprite != null)
            {
                Context.AssetMemory.DisposeAsset(currentImageSpritePath, AssetTag.DialogueImage);
                currentImageSprite = null;
                currentImageSpritePath = null;
            }
        }

        string ReplacePlaceholders(string str)
        {
            foreach (var (placeholder, function) in PlaceholderFunctions)
            {
                if (str.Contains(placeholder))
                {
                    str = str.Replace(placeholder, function());
                }
            }
            return str;
        }
    }
    
    private static readonly Dictionary<string, Func<string>> PlaceholderFunctions = new Dictionary<string, Func<string>>
    {
        {"[BADGE_TITLE]", () => CurrentBadge.title},
        {"[BADGE_DESCRIPTION]", () => CurrentBadge.description},
        {"[BADGE_IMAGE_URL]", () => CurrentBadge.GetImageUrl()},
        {"[N/A]", () => ""}
    };

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
            if (GUILayout.Button("Intro"))
            {
                var intro = Resources.Load<TextAsset>("Stories/Intro");
                LevelSelectionScreen.HighlightedLevelId = BuiltInData.TutorialLevelId;
                var story = new Story(intro.text);
                story.variablesState["IsBeginner"] = true;
                DialogueOverlay.Show(story);
                //LevelSelectionScreen.HighlightedLevelId = null;
            }
            if (GUILayout.Button("Badge"))
            {
                DialogueOverlay.CurrentBadge = new Badge
                {
                    title = "夏祭：一段",
                    description = "测试\n你好\n贵阳！\n11111111111",
                    type = BadgeType.Event,
                    metadata = new Dictionary<string, object>
                    {
                        {"imageUrl", "http://artifacts.cytoid.io/badges/sora1.jpg"}
                    }
                };
                var badge = Resources.Load<TextAsset>("Stories/Badge");
                var story = new Story(badge.text);
                DialogueOverlay.Show(story);
            }
        }
    }
}
#endif