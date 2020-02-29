using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class TierCard : MonoBehaviour
{
    [GetComponent] public RectTransform rectTransform;
    [GetComponent] public CanvasGroup canvasGroup;
    [GetComponent] public Canvas canvas;

    public CharacterBackdrop characterBackdrop;
    public Text titleText;
    public Text completionPercentageText;
    public TierStageCard[] stageCards;
    public GameObject lockedOverlayRoot;
    public Image lockedOverlayIcon;
    public Text lockedOverlayText;
    public GameObject criteriaHolder;
    public CriterionEntry criterionEntryPrefab;
    public Image characterImage;
    public Image cardOverlayImage;
    public GradientMeshEffect backgroundGradient;

    public int Index { get; private set; }
    public bool IsScrollRectFix = false;
    private Vector2 screenCenter;
    private bool active;
    private CancellationTokenSource characterPreviewToken;

    private void Awake()
    {
        canvasGroup.alpha = 0;
        characterImage.SetAlpha(0);
    }

    public void ScrollCellContent(object obj)
    {
        var data = (UserTier) obj;
        IsScrollRectFix = data.isScrollRectFix;
        if (IsScrollRectFix)
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }
        }
        else
        {
            Index = data.index;
            canvasGroup.alpha = 1f;
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(true);
            }

            active = true;
            screenCenter = this.GetScreenParent<TierSelectionScreen>().ScreenCenter;

            characterBackdrop.gameObject.SetActive(data.tier.character != null);
            if (data.tier.character != null)
            {
                LoadCharacterPreview(data.locked
                    ? data.tier.character.silhouetteURL
                    : data.tier.character.thumbnailURL);
            }

            lockedOverlayRoot.SetActive(Index > 0);
        }
    }

    public async void LoadCharacterPreview(string uri)
    {
        if (characterPreviewToken != null
            && !characterPreviewToken.IsCancellationRequested)
        {
            characterPreviewToken.Cancel();
            characterPreviewToken = null;
        }
        
        characterPreviewToken = new CancellationTokenSource();
        Sprite sprite;
        try
        {
            sprite = await Context.SpriteCache.CacheSpriteInMemory(
                uri,
                "CharacterPreview",
                characterPreviewToken.Token,
                useFileCache: true);
        }
        catch
        {
            characterImage.sprite = null;
            characterImage.SetAlpha(0);
            return;
        }

        if (sprite != null)
        {
            characterImage.sprite = sprite;
            characterImage.SetAlpha(1);
        }
    }

    public void ScrollCellReturn()
    {
        active = false;
        if (characterPreviewToken != null
            && !characterPreviewToken.IsCancellationRequested)
        {
            characterPreviewToken.Cancel();
            characterPreviewToken = null;
        }
    }

    public void Update()
    {
        if (!active) return;
        var t = Mathf.Clamp01(Math.Abs(rectTransform.GetScreenSpaceCenter().y - screenCenter.y) / 540);
        var a = 0.4f + Mathf.Lerp(0.8f, 0, t);
        cardOverlayImage.SetAlpha(1 - a);
        if (characterImage.sprite != null)
        {
            characterImage.color = Color.Lerp(Color.white, Color.black, 1 - a);
        }
        rectTransform.localScale = Vector3.one * (0.9f + Mathf.Lerp(0.1f, 0, t));
        if (t < 0.5f)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 10;
        }
        else
        {
            canvas.overrideSorting = false;
        }
    }
}