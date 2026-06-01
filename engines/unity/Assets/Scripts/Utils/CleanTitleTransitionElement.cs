using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CleanTitleTransitionElement : InteractableMonoBehavior
{
    public CanvasGroup canvasGroup;
    public bool entryOnScreenBecameActive;
    public bool exitOnScreenBecameInactive;

    public CttStyle style;

    // Style 3 & 9 children
    public RectTransform topMask;
    public RectTransform bottomMask;
    public Image outlineFill;
    public Image circleFill;
    public RectTransform borderLine;

    // Style 11 children
    public RectTransform borderLeft;
    public RectTransform borderRight;
    public RectTransform borderLeftTop;
    public RectTransform borderLeftBottom;
    public RectTransform borderRightTop;
    public RectTransform borderRightBottom;
    public RectTransform leftCorner;
    public RectTransform rightCorner;
    public RectTransform leftTopCorner;
    public RectTransform rightTopCorner;
    public RectTransform textMask;

    public enum CttStyle
    {
        Style3 = 3,
        Style9Modified = 9,
        Style11 = 11,
    }

    private Tween currentTween;
    private CancellationTokenSource animateCancelSource;

    private void OnValidate()
    {
        this.AutoFill(ref canvasGroup);
    }

    protected virtual void Awake()
    {
        this.AutoFill(ref canvasGroup);
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        var screen = this.GetScreenParent();
        if (screen != null)
        {
            screen.onScreenBecameActive.AddListener(OnScreenBecameActive);
            screen.onScreenBecameInactive.AddListener(OnScreenBecameInactive);
        }
    }

    public async void OnScreenBecameActive()
    {
        if (entryOnScreenBecameActive) await Animate();
    }

    public async void OnScreenBecameInactive()
    {
        if (exitOnScreenBecameInactive) await Animate(false);
    }

    public async UniTask Animate(bool entry = true)
    {
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;

        currentTween?.Kill();
        animateCancelSource?.Cancel();
        animateCancelSource = new CancellationTokenSource();

        try
        {
            if (entry)
            {
                var expandSeq = BuildExpandSequence();
                currentTween = expandSeq;
                await PlaySequence(expandSeq);

                if (style == CttStyle.Style3 || style == CttStyle.Style11)
                {
                    var holdMs = style == CttStyle.Style3 ? 2750 : 1920;
                    await UniTask.Delay(holdMs, cancellationToken: animateCancelSource.Token);

                    var closeSeq = BuildCloseSequence();
                    currentTween = closeSeq;
                    await PlaySequence(closeSeq);
                }
            }
            else
            {
                var closeSeq = BuildCloseSequence();
                currentTween = closeSeq;
                await PlaySequence(closeSeq);
            }

            if (!entry || style == CttStyle.Style3 || style == CttStyle.Style11)
            {
                canvasGroup.alpha = 0;
                canvasGroup.blocksRaycasts = false;
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
    }

    private async UniTask PlaySequence(Sequence seq)
    {
        if (seq == null || !seq.IsActive()) return;
        var completionTask = seq.AsyncWaitForCompletion();
        while (!completionTask.IsCompleted && !animateCancelSource.IsCancellationRequested)
        {
            await UniTask.Yield(animateCancelSource.Token);
        }
    }

    private Sequence BuildExpandSequence()
    {
        switch (style)
        {
            case CttStyle.Style3: return BuildStyle3Expand();
            case CttStyle.Style9Modified: return BuildStyle9Expand();
            case CttStyle.Style11: return BuildStyle11Expand();
            default: return null;
        }
    }

    private Sequence BuildCloseSequence()
    {
        switch (style)
        {
            case CttStyle.Style3: return BuildStyle3Close();
            case CttStyle.Style9Modified: return BuildStyle9Close();
            case CttStyle.Style11: return BuildStyle11Close();
            default: return null;
        }
    }

    // ============================================================
    //  Style 3
    // ============================================================

    private Sequence BuildStyle3Expand()
    {
        // Reset to t=0 values
        outlineFill.fillAmount = 0f;
        outlineFill.fillClockwise = true;
        circleFill.fillAmount = 0f;
        circleFill.fillClockwise = true;
        topMask.anchoredPosition = new Vector2(topMask.anchoredPosition.x, 0f);
        topMask.sizeDelta = new Vector2(topMask.sizeDelta.x, -500f);
        bottomMask.anchoredPosition = new Vector2(bottomMask.anchoredPosition.x, 0f);
        bottomMask.sizeDelta = new Vector2(bottomMask.sizeDelta.x, -500f);

        var seq = DOTween.Sequence();
        seq.SetRecyclable(true);

        // Outline fillAmount: 0 → 0.9 (0.00–0.17), 0.9 → 1.0 (0.17–0.42)
        seq.Insert(0.00f, outlineFill.DOFillAmount(0.9f, 0.17f).SetEase(Ease.Linear));
        seq.Insert(0.17f, outlineFill.DOFillAmount(1.0f, 0.25f).SetEase(Ease.Linear));

        // Circle fillAmount: 0 → 0 hold (0.00–0.33), 0 → 0.8 (0.33–0.50), 0.8 → 1.0 (0.50–0.67)
        seq.Insert(0.33f, circleFill.DOFillAmount(0.8f, 0.17f).SetEase(Ease.Linear));
        seq.Insert(0.50f, circleFill.DOFillAmount(1.0f, 0.17f).SetEase(Ease.Linear));

        // TopMask position y: 0 → 117.5 (0.50–0.67), 117.5 → 125 (0.67–0.83)
        seq.Insert(0.50f, topMask.DOAnchorPosY(117.5f, 0.17f).SetEase(Ease.Linear));
        seq.Insert(0.67f, topMask.DOAnchorPosY(125f, 0.16f).SetEase(Ease.Linear));

        // TopMask sizeDelta y: -500 → -265 (0.50–0.67), -265 → -250 (0.67–0.83)
        seq.Insert(0.50f, DOTween.To(
            () => topMask.sizeDelta,
            v => topMask.sizeDelta = v,
            new Vector2(topMask.sizeDelta.x, -265f), 0.17f).SetEase(Ease.Linear));
        seq.Insert(0.67f, DOTween.To(
            () => topMask.sizeDelta,
            v => topMask.sizeDelta = v,
            new Vector2(topMask.sizeDelta.x, -250f), 0.16f).SetEase(Ease.Linear));

        // BottomMask position y: 0 → -117.5 (0.67–0.83), -117.5 → -125 (0.83–1.00)
        seq.Insert(0.67f, bottomMask.DOAnchorPosY(-117.5f, 0.16f).SetEase(Ease.Linear));
        seq.Insert(0.83f, bottomMask.DOAnchorPosY(-125f, 0.17f).SetEase(Ease.Linear));

        // BottomMask sizeDelta y: -500 → -265 (0.67–0.83), -265 → -250 (0.83–1.00)
        seq.Insert(0.67f, DOTween.To(
            () => bottomMask.sizeDelta,
            v => bottomMask.sizeDelta = v,
            new Vector2(bottomMask.sizeDelta.x, -265f), 0.16f).SetEase(Ease.Linear));
        seq.Insert(0.83f, DOTween.To(
            () => bottomMask.sizeDelta,
            v => bottomMask.sizeDelta = v,
            new Vector2(bottomMask.sizeDelta.x, -250f), 0.17f).SetEase(Ease.Linear));

        return seq;
    }

    private Sequence BuildStyle3Close()
    {
        // Reset to expanded (end) state — Close clip played in reverse (speed=-1)
        outlineFill.fillAmount = 1f;
        outlineFill.fillClockwise = false;
        circleFill.fillAmount = 1f;
        circleFill.fillClockwise = false;
        topMask.anchoredPosition = new Vector2(topMask.anchoredPosition.x, 125f);
        topMask.sizeDelta = new Vector2(topMask.sizeDelta.x, -250f);
        bottomMask.anchoredPosition = new Vector2(bottomMask.anchoredPosition.x, -125f);
        bottomMask.sizeDelta = new Vector2(bottomMask.sizeDelta.x, -250f);

        var seq = DOTween.Sequence();
        seq.SetRecyclable(true);

        // BottomMask position y: -125 → -117.5 (0.00–0.17), -117.5 → 0 (0.17–0.33)
        seq.Insert(0.00f, bottomMask.DOAnchorPosY(-117.5f, 0.17f).SetEase(Ease.Linear));
        seq.Insert(0.17f, bottomMask.DOAnchorPosY(0f, 0.16f).SetEase(Ease.Linear));

        // BottomMask sizeDelta y: -250 → -265 (0.00–0.17), -265 → -500 (0.17–0.33)
        seq.Insert(0.00f, DOTween.To(
            () => bottomMask.sizeDelta,
            v => bottomMask.sizeDelta = v,
            new Vector2(bottomMask.sizeDelta.x, -265f), 0.17f).SetEase(Ease.Linear));
        seq.Insert(0.17f, DOTween.To(
            () => bottomMask.sizeDelta,
            v => bottomMask.sizeDelta = v,
            new Vector2(bottomMask.sizeDelta.x, -500f), 0.16f).SetEase(Ease.Linear));

        // TopMask position y: 125 → 117.5 (0.17–0.33), 117.5 → 0 (0.33–0.50)
        seq.Insert(0.17f, topMask.DOAnchorPosY(117.5f, 0.16f).SetEase(Ease.Linear));
        seq.Insert(0.33f, topMask.DOAnchorPosY(0f, 0.17f).SetEase(Ease.Linear));

        // TopMask sizeDelta y: -250 → -265 (0.17–0.33), -265 → -500 (0.33–0.50)
        seq.Insert(0.17f, DOTween.To(
            () => topMask.sizeDelta,
            v => topMask.sizeDelta = v,
            new Vector2(topMask.sizeDelta.x, -265f), 0.16f).SetEase(Ease.Linear));
        seq.Insert(0.33f, DOTween.To(
            () => topMask.sizeDelta,
            v => topMask.sizeDelta = v,
            new Vector2(topMask.sizeDelta.x, -500f), 0.17f).SetEase(Ease.Linear));

        // Circle fillAmount: 1.0 → 0.8 (0.33–0.50), 0.8 → 0 (0.50–0.67)
        seq.Insert(0.33f, circleFill.DOFillAmount(0.8f, 0.17f).SetEase(Ease.Linear));
        seq.Insert(0.50f, circleFill.DOFillAmount(0f, 0.17f).SetEase(Ease.Linear));

        // Outline fillAmount: 1.0 → 0.2 (0.58–0.83), 0.2 → 0 (0.83–1.00)
        seq.Insert(0.58f, outlineFill.DOFillAmount(0.2f, 0.25f).SetEase(Ease.Linear));
        seq.Insert(0.83f, outlineFill.DOFillAmount(0f, 0.17f).SetEase(Ease.Linear));

        return seq;
    }

    // ============================================================
    //  Style 9 Modified
    // ============================================================

    private Sequence BuildStyle9Expand()
    {
        borderLine.localScale = new Vector3(0, borderLine.localScale.y, borderLine.localScale.z);
        topMask.sizeDelta = new Vector2(topMask.sizeDelta.x, 0);
        bottomMask.sizeDelta = new Vector2(bottomMask.sizeDelta.x, 0);

        var seq = DOTween.Sequence();
        seq.SetRecyclable(true);

        // BorderLine scaleX: 0 → 0.8 (0.00–0.17), 0.8 → 1.0 (0.17–0.42)
        seq.Insert(0.00f, DOTween.To(
            () => borderLine.localScale.x,
            x => borderLine.localScale = new Vector3(x, borderLine.localScale.y, borderLine.localScale.z),
            0.8f, 0.17f).SetEase(Ease.Linear));
        seq.Insert(0.17f, DOTween.To(
            () => borderLine.localScale.x,
            x => borderLine.localScale = new Vector3(x, borderLine.localScale.y, borderLine.localScale.z),
            1.0f, 0.25f).SetEase(Ease.Linear));

        // TopMask sizeDelta y: 0 hold (0.00–0.33), 0 → 115 (0.33–0.50), 115 → 125 (0.50–0.75)
        seq.Insert(0.33f, DOTween.To(
            () => topMask.sizeDelta,
            v => topMask.sizeDelta = v,
            new Vector2(topMask.sizeDelta.x, 115f), 0.17f).SetEase(Ease.Linear));
        seq.Insert(0.50f, DOTween.To(
            () => topMask.sizeDelta,
            v => topMask.sizeDelta = v,
            new Vector2(topMask.sizeDelta.x, 125f), 0.25f).SetEase(Ease.Linear));

        // BottomMask sizeDelta y: 0 hold (0.00–0.50), 0 → 115 (0.50–0.67), 115 → 125 (0.67–0.92)
        seq.Insert(0.50f, DOTween.To(
            () => bottomMask.sizeDelta,
            v => bottomMask.sizeDelta = v,
            new Vector2(bottomMask.sizeDelta.x, 115f), 0.17f).SetEase(Ease.Linear));
        seq.Insert(0.67f, DOTween.To(
            () => bottomMask.sizeDelta,
            v => bottomMask.sizeDelta = v,
            new Vector2(bottomMask.sizeDelta.x, 125f), 0.25f).SetEase(Ease.Linear));

        return seq;
    }

    private Sequence BuildStyle9Close()
    {
        borderLine.localScale = new Vector3(1, borderLine.localScale.y, borderLine.localScale.z);
        topMask.sizeDelta = new Vector2(topMask.sizeDelta.x, 125);
        bottomMask.sizeDelta = new Vector2(bottomMask.sizeDelta.x, 125);

        var seq = DOTween.Sequence();
        seq.SetRecyclable(true);

        // BorderLine scaleX: 1 hold (0.00–0.17), 1 → 0.2 (0.17–0.33), 0.2 → 0 (0.33–0.50)
        seq.Insert(0.17f, DOTween.To(
            () => borderLine.localScale.x,
            x => borderLine.localScale = new Vector3(x, borderLine.localScale.y, borderLine.localScale.z),
            0.2f, 0.16f).SetEase(Ease.Linear));
        seq.Insert(0.33f, DOTween.To(
            () => borderLine.localScale.x,
            x => borderLine.localScale = new Vector3(x, borderLine.localScale.y, borderLine.localScale.z),
            0f, 0.17f).SetEase(Ease.Linear));

        // TopMask sizeDelta y: 125 → 25 (0.00–0.17), 25 → 0 (0.17–0.42)
        seq.Insert(0.00f, DOTween.To(
            () => topMask.sizeDelta,
            v => topMask.sizeDelta = v,
            new Vector2(topMask.sizeDelta.x, 25f), 0.17f).SetEase(Ease.Linear));
        seq.Insert(0.17f, DOTween.To(
            () => topMask.sizeDelta,
            v => topMask.sizeDelta = v,
            new Vector2(topMask.sizeDelta.x, 0f), 0.25f).SetEase(Ease.Linear));

        // BottomMask sizeDelta y: 125 hold (0.00–0.08), 125 → 25 (0.08–0.25), 25 → 0 (0.25–0.50)
        seq.Insert(0.08f, DOTween.To(
            () => bottomMask.sizeDelta,
            v => bottomMask.sizeDelta = v,
            new Vector2(bottomMask.sizeDelta.x, 25f), 0.17f).SetEase(Ease.Linear));
        seq.Insert(0.25f, DOTween.To(
            () => bottomMask.sizeDelta,
            v => bottomMask.sizeDelta = v,
            new Vector2(bottomMask.sizeDelta.x, 0f), 0.25f).SetEase(Ease.Linear));

        return seq;
    }

    // ============================================================
    //  Style 11
    // ============================================================

    private Sequence BuildStyle11Expand()
    {

        // Reset Phase 1
        SetScaleX(borderLeftTop, 0);
        SetScaleX(borderRightTop, 0);
        SetScaleY(borderLeftBottom, 0);
        SetScaleY(borderRightBottom, 0);

        // Reset Phase 2
        borderLeft.anchoredPosition = new Vector2(20f, borderLeft.anchoredPosition.y);
        borderRight.anchoredPosition = new Vector2(-20f, borderRight.anchoredPosition.y);

        // Reset Phase 3
        leftCorner.gameObject.SetActive(false);
        rightCorner.gameObject.SetActive(false);
        leftTopCorner.gameObject.SetActive(false);
        rightTopCorner.gameObject.SetActive(false);
        borderLeft.gameObject.SetActive(true);
        borderRight.gameObject.SetActive(true);

        // Reset Phase 4
        leftCorner.anchoredPosition = new Vector2(225f, -62.5f);
        rightCorner.anchoredPosition = new Vector2(-225f, 62.5f);
        leftTopCorner.anchoredPosition = new Vector2(225f, 62.5f);
        rightTopCorner.anchoredPosition = new Vector2(-225f, -62.5f);
        textMask.sizeDelta = new Vector2(-500f, -140f);
        textMask.anchoredPosition = new Vector2(textMask.anchoredPosition.x, 0);

        var seq = DOTween.Sequence();
        seq.SetRecyclable(true);

        // Phase 1: Border pieces scale in (0.00–0.42)
        seq.Insert(0.00f, DOTween.To(
            () => borderLeftTop.localScale.x,
            x => SetScaleX(borderLeftTop, x),
            0.8f, 0.08f).SetEase(Ease.Linear));
        seq.Insert(0.08f, DOTween.To(
            () => borderLeftTop.localScale.x,
            x => SetScaleX(borderLeftTop, x),
            1.0f, 0.17f).SetEase(Ease.Linear));

        seq.Insert(0.00f, DOTween.To(
            () => borderRightTop.localScale.x,
            x => SetScaleX(borderRightTop, x),
            0.8f, 0.08f).SetEase(Ease.Linear));
        seq.Insert(0.08f, DOTween.To(
            () => borderRightTop.localScale.x,
            x => SetScaleX(borderRightTop, x),
            1.0f, 0.17f).SetEase(Ease.Linear));

        seq.Insert(0.25f, DOTween.To(
            () => borderLeftBottom.localScale.y,
            y => SetScaleY(borderLeftBottom, y),
            1.0f, 0.17f).SetEase(Ease.Linear));

        seq.Insert(0.25f, DOTween.To(
            () => borderRightBottom.localScale.y,
            y => SetScaleY(borderRightBottom, y),
            1.0f, 0.17f).SetEase(Ease.Linear));

        // Phase 2: Border slides inward (0.42–0.67)
        seq.Insert(0.42f, borderLeft.DOAnchorPosX(5f, 0.08f).SetEase(Ease.Linear));
        seq.Insert(0.50f, borderLeft.DOAnchorPosX(0f, 0.17f).SetEase(Ease.Linear));
        seq.Insert(0.42f, borderRight.DOAnchorPosX(-5f, 0.08f).SetEase(Ease.Linear));
        seq.Insert(0.50f, borderRight.DOAnchorPosX(0f, 0.17f).SetEase(Ease.Linear));

        // Phase 3: Corners activate, borders deactivate (0.70–0.75)
        seq.InsertCallback(0.70f, () =>
        {
            leftCorner.gameObject.SetActive(true);
            rightCorner.gameObject.SetActive(true);
            leftTopCorner.gameObject.SetActive(true);
            rightTopCorner.gameObject.SetActive(true);
        });
        seq.InsertCallback(0.75f, () =>
        {
            borderLeft.gameObject.SetActive(false);
            borderRight.gameObject.SetActive(false);
        });

        // Phase 4: Corners slide in, TextMask opens (0.75–1.167)
        seq.Insert(0.75f, leftCorner.DOAnchorPos(new Vector2(20f, -10f), 0.17f).SetEase(Ease.Linear));
        seq.Insert(0.92f, leftCorner.DOAnchorPos(Vector2.zero, 0.247f).SetEase(Ease.Linear));

        seq.Insert(0.75f, rightCorner.DOAnchorPos(new Vector2(-20f, 10f), 0.17f).SetEase(Ease.Linear));
        seq.Insert(0.92f, rightCorner.DOAnchorPos(Vector2.zero, 0.247f).SetEase(Ease.Linear));

        seq.Insert(0.75f, leftTopCorner.DOAnchorPos(new Vector2(20f, 10f), 0.17f).SetEase(Ease.Linear));
        seq.Insert(0.92f, leftTopCorner.DOAnchorPos(Vector2.zero, 0.247f).SetEase(Ease.Linear));

        seq.Insert(0.75f, rightTopCorner.DOAnchorPos(new Vector2(-20f, -10f), 0.17f).SetEase(Ease.Linear));
        seq.Insert(0.92f, rightTopCorner.DOAnchorPos(Vector2.zero, 0.247f).SetEase(Ease.Linear));

        seq.Insert(0.75f, DOTween.To(
            () => textMask.sizeDelta,
            v => textMask.sizeDelta = v,
            new Vector2(-60f, -40f), 0.17f).SetEase(Ease.Linear));
        seq.Insert(0.92f, DOTween.To(
            () => textMask.sizeDelta,
            v => textMask.sizeDelta = v,
            new Vector2(-20f, -20f), 0.247f).SetEase(Ease.Linear));

        return seq;
    }

    private Sequence BuildStyle11Close()
    {

        // Reset to expanded end state first, then reverse
        SetScaleX(borderLeftTop, 1f);
        SetScaleX(borderRightTop, 1f);
        SetScaleY(borderLeftBottom, 1f);
        SetScaleY(borderRightBottom, 1f);

        borderLeft.gameObject.SetActive(false);
        borderRight.gameObject.SetActive(false);
        leftCorner.gameObject.SetActive(true);
        rightCorner.gameObject.SetActive(true);
        leftTopCorner.gameObject.SetActive(true);
        rightTopCorner.gameObject.SetActive(true);

        leftCorner.anchoredPosition = Vector2.zero;
        rightCorner.anchoredPosition = Vector2.zero;
        leftTopCorner.anchoredPosition = Vector2.zero;
        rightTopCorner.anchoredPosition = Vector2.zero;
        textMask.sizeDelta = new Vector2(-20f, -20f);
        textMask.anchoredPosition = new Vector2(textMask.anchoredPosition.x, 0);

        var seq = DOTween.Sequence();
        seq.SetRecyclable(true);

        // Reverse Phase 4: Corners slide out, TextMask closes (0.00–0.417)
        seq.Insert(0.00f, leftCorner.DOAnchorPos(new Vector2(225f, -62.5f), 0.417f).SetEase(Ease.Linear));
        seq.Insert(0.00f, rightCorner.DOAnchorPos(new Vector2(-225f, 62.5f), 0.417f).SetEase(Ease.Linear));
        seq.Insert(0.00f, leftTopCorner.DOAnchorPos(new Vector2(225f, 62.5f), 0.417f).SetEase(Ease.Linear));
        seq.Insert(0.00f, rightTopCorner.DOAnchorPos(new Vector2(-225f, -62.5f), 0.417f).SetEase(Ease.Linear));
        seq.Insert(0.00f, DOTween.To(
            () => textMask.sizeDelta,
            v => textMask.sizeDelta = v,
            new Vector2(-500f, -140f), 0.417f).SetEase(Ease.Linear));

        // Reverse Phase 3: Corners deactivate, borders reactivate (0.417–0.467)
        seq.InsertCallback(0.417f, () =>
        {
            borderLeft.gameObject.SetActive(true);
            borderRight.gameObject.SetActive(true);
        });
        seq.InsertCallback(0.467f, () =>
        {
            leftCorner.gameObject.SetActive(false);
            rightCorner.gameObject.SetActive(false);
            leftTopCorner.gameObject.SetActive(false);
            rightTopCorner.gameObject.SetActive(false);
        });

        // Reverse Phase 2: Border slides outward (0.467–0.717)
        borderLeft.anchoredPosition = Vector2.zero;
        borderRight.anchoredPosition = Vector2.zero;
        seq.Insert(0.467f, borderLeft.DOAnchorPosX(20f, 0.25f).SetEase(Ease.Linear));
        seq.Insert(0.467f, borderRight.DOAnchorPosX(-20f, 0.25f).SetEase(Ease.Linear));

        // Reverse Phase 1: Border pieces scale out (0.717–1.167)
        seq.Insert(0.717f, DOTween.To(
            () => borderLeftBottom.localScale.y,
            y => SetScaleY(borderLeftBottom, y),
            0f, 0.17f).SetEase(Ease.Linear));
        seq.Insert(0.717f, DOTween.To(
            () => borderRightBottom.localScale.y,
            y => SetScaleY(borderRightBottom, y),
            0f, 0.17f).SetEase(Ease.Linear));

        seq.Insert(0.887f, DOTween.To(
            () => borderLeftTop.localScale.x,
            x => SetScaleX(borderLeftTop, x),
            0.8f, 0.17f).SetEase(Ease.Linear));
        seq.Insert(0.887f, DOTween.To(
            () => borderRightTop.localScale.x,
            x => SetScaleX(borderRightTop, x),
            0.8f, 0.17f).SetEase(Ease.Linear));

        seq.Insert(1.057f, DOTween.To(
            () => borderLeftTop.localScale.x,
            x => SetScaleX(borderLeftTop, x),
            0f, 0.11f).SetEase(Ease.Linear));
        seq.Insert(1.057f, DOTween.To(
            () => borderRightTop.localScale.x,
            x => SetScaleX(borderRightTop, x),
            0f, 0.11f).SetEase(Ease.Linear));

        return seq;
    }

    // ============================================================
    //  Helpers
    // ============================================================

    private static void SetScaleX(Transform t, float x)
    {
        var s = t.localScale;
        s.x = x;
        t.localScale = s;
    }

    private static void SetScaleY(Transform t, float y)
    {
        var s = t.localScale;
        s.y = y;
        t.localScale = s;
    }
}
