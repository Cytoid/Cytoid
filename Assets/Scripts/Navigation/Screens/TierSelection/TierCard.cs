using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TierCard : MonoBehaviour
{
    [GetComponent] public RectTransform rectTransform;
    [GetComponent] public CanvasGroup canvasGroup;
    [GetComponent] public Canvas canvas;

    public GameObject characterRoot;
    public CharacterDisplay characterDisplay;
    public TierGradientPane gradientPane;
    public TierStageCard[] stageCards;
    public GameObject lockedOverlayRoot;
    public Image lockedOverlayIcon;
    public Sprite lockedIcon;
    public Sprite unlockedIcon;
    public Text lockedOverlayText;
    public GameObject criteriaHolder;
    public CriterionEntry criterionEntryPrefab;
    
    public Image cardOverlayImage;
    
    public TierData Tier { get; private set; }
    public int Index { get; private set; }
    public bool IsScrollRectFix { get; set; }

    private Vector2 screenCenter;
    private bool active;
    private bool fadedOut;

    private void Awake()
    {
        canvasGroup.alpha = 0;
    }

    private void OnDestroy()
    {
        characterDisplay.Unload();
    }

    public void ScrollCellContent(object obj) => SetModel((TierData) obj);

    public void SetModel(TierData tier)
    {
        Tier = tier;
        IsScrollRectFix = tier.isScrollRectFix;
        if (IsScrollRectFix)
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }
        }
        else
        {
            Index = tier.index;
            canvasGroup.alpha = 1f;
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(true);
            }

            active = true;
            screenCenter = this.GetScreenParent<TierSelectionScreen>().ScreenCenter;

            characterRoot.gameObject.SetActive(tier.Meta.character != null);
            if (tier.Meta.character != null)
            {
                characterDisplay.Load(tier.Meta.character.TachieAssetId);
                characterDisplay.canvasGroup.alpha = 0;
            }

            gradientPane.SetModel(tier);

            for (var stage = 0; stage < Math.Min(3, tier.Meta.stages.Count); stage++)
            {
                stageCards[stage].SetModel(
                    tier.Meta.parsedStages[stage], 
                    new ColorGradient(tier.Meta.colorPalette.stages[stage], 90f)
                );
            }
            
            lockedOverlayRoot.SetActive(tier.locked || !tier.StagesValid);
            if (tier.locked)
            {
                lockedOverlayIcon.sprite = lockedIcon;
                lockedOverlayText.text = "Locked";
            } 
            else if (!tier.StagesValid)
            {
                lockedOverlayIcon.sprite = unlockedIcon;
                lockedOverlayText.text = "Not downloaded";
            }

            foreach (Transform child in criteriaHolder.transform) Destroy(child.gameObject);
            foreach (var criterion in tier.Meta.parsedCriteria)
            {
                var criterionEntry = Instantiate(criterionEntryPrefab, criteriaHolder.transform);
                criterionEntry.SetModel(criterion.Description, CriterionState.Passed);
            }

            LayoutFixer.Fix(criteriaHolder.transform);
        }
    }

    public void ScrollCellReturn()
    {
        active = false;
        characterDisplay.Unload();
    }

    public void Update()
    {
        if (!active || fadedOut) return;
        var t = Mathf.Clamp01(Math.Abs(rectTransform.GetScreenSpaceCenter(canvas).y - screenCenter.y) / 540);
        var a = 0.4f + Mathf.Lerp(0.8f, 0, t);
        cardOverlayImage.SetAlpha(1 - a);
        if (characterDisplay.IsLoaded)
        {
            characterDisplay.canvasGroup.alpha = a;
        }
        rectTransform.localScale = Vector3.one * (0.9f + Mathf.Lerp(0.1f, 0, t));
        if (t < 0.5f)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 9;
        }
        else
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 8;
        }
    }

    public void OnTierStart()
    {
        fadedOut = true;
        var t = Mathf.Clamp01(Math.Abs(rectTransform.GetScreenSpaceCenter(canvas).y - screenCenter.y) / 540);
        if (t < 0.5f)
        {
            DOTween.Sequence()
                .Append(rectTransform.DOScale(Vector3.one * 0.95f, 0.3f).SetEase(Ease.OutCubic))
                .Append(rectTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.InCubic));
        }
        else
        {
            cardOverlayImage.DOFade(1, 0.4f);
            if (characterDisplay.IsLoaded)
            {
                characterDisplay.canvasGroup.DOFade(0, 0.4f);
            }
        }
    }
}