using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FollowStub : MonoBehaviour
{
    
    public RectTransform stub;
    public float tweenDuration = 0.2f;

    private RectTransform self;

    private void Awake()
    {
        self = GetComponent<RectTransform>();
        var layoutGroup = GetComponent<LayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.enabled = false;
        }

        var contentSizeFitter = GetComponent<ContentSizeFitter>();
        if (contentSizeFitter != null)
        {
            contentSizeFitter.enabled = false;
        }
    }

    private void Update()
    {
        if (self.anchorMin != stub.anchorMin) self.anchorMin = stub.anchorMin;
        if (self.anchorMax != stub.anchorMax) self.anchorMax = stub.anchorMax;
        if (self.pivot != stub.pivot) self.pivot = stub.pivot;
        
        if (self.anchoredPosition != stub.anchoredPosition)
        {
            self.DOAnchorPos(stub.anchoredPosition, tweenDuration).SetEase(Ease.OutCubic);
        }

        if (self.sizeDelta != stub.sizeDelta)
        {
            self.DOSizeDelta(stub.sizeDelta, tweenDuration).SetEase(Ease.OutCubic);
        }
    }
}