using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PulseElement : MonoBehaviour
{
    private static bool cloning;

    public float initialAlpha = 0.6f;
    public float finalSize = 2f;
    public bool overrideFinalSizeY = false;
    public float finalSizeY = -1;
    public float duration = 3f;
    public bool overlay = false;

    public Ease ease = Ease.OutCubic;
    public List<MonoBehaviour> componentsToDestroyAfterClone = new List<MonoBehaviour>();

    public List<Type> typesToDestroyAfterClone = new List<Type>
    {
        typeof(TransitionElement), typeof(InteractableMonoBehavior)
    };

    private bool isCloned;

    private GameObject holder;
    private RectTransform holderRectTransform;
    private RectTransform rectTransform;

    protected void Awake()
    {
        if (cloning)
        {
            isCloned = true;
            return;
        }

        holder = new GameObject(gameObject.name + "_PulseWrapper");
        holder.transform.parent = transform.parent;
        holder.transform.SetZ(transform.position.z);

        holderRectTransform = holder.AddComponent<RectTransform>();
        rectTransform = GetComponent<RectTransform>();

        holderRectTransform.pivot = rectTransform.pivot;
        holderRectTransform.SetSiblingIndex(transform.GetSiblingIndex());
        holderRectTransform.sizeDelta = rectTransform.sizeDelta;
        holderRectTransform.anchorMax = rectTransform.anchorMax;
        holderRectTransform.anchorMin = rectTransform.anchorMin;
        holderRectTransform.offsetMax = rectTransform.offsetMax;
        holderRectTransform.offsetMin = rectTransform.offsetMin;
        holderRectTransform.anchoredPosition = rectTransform.anchoredPosition;
        holderRectTransform.localScale = Vector3.one;

        transform.SetParent(holder.transform, false);

        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(0, 0);
        rectTransform.anchoredPosition = new Vector2(0, 0);
        rectTransform.offsetMax = new Vector2(0, 0);
        rectTransform.offsetMin = new Vector2(0, 0);
    }

    private void Update()
    {
        if (isCloned) return;

        var newHolderSizeDelta = holderRectTransform.sizeDelta;
        var rectSizeDelta = rectTransform.sizeDelta;
        newHolderSizeDelta.x += rectSizeDelta.x;
        newHolderSizeDelta.y += rectSizeDelta.y;
        holderRectTransform.sizeDelta = newHolderSizeDelta;
    }

    public void Pulse()
    {
        if (holder == null)
        {
            throw new InvalidOperationException("Pulse element not initialized yet");
        }

        cloning = true;
        var clone = Instantiate(gameObject, holder.transform);
        clone.name = "Pulse";
        Destroy(clone.GetComponent<PulseElement>());
        cloning = false;

        if (overlay) clone.transform.SetAsLastSibling(); else clone.transform.SetAsFirstSibling();
        foreach (var component in clone.gameObject.GetComponentsInChildren<MonoBehaviour>())
        {
            if (componentsToDestroyAfterClone.Any(it => it.GetType() == component.GetType()))
            {
                Destroy(component);
            }
            else if (typesToDestroyAfterClone.Any(it => it == component.GetType()))
            {
                Destroy(component);
            }
        }

        var canvasGroup = clone.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = clone.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0;

        PostPulse(clone, canvasGroup);
    }

    private async void PostPulse(GameObject clone, CanvasGroup canvasGroup)
    {
        if (this == null || canvasGroup == null || clone == null) return;

        var cloneRectTransform = clone.GetComponent<RectTransform>();

        var equal = false;
        while (!equal)
        {
            if (clone == null) return;
            clone.transform.RebuildLayout();
            equal = cloneRectTransform.rect == rectTransform.rect;
            await UniTask.Yield();
        }

        canvasGroup.alpha = initialAlpha;
        canvasGroup.DOFade(0, duration);
        if (overrideFinalSizeY)
        {
            clone.GetComponent<RectTransform>().Apply(it =>
            {
                it.DOScaleX(finalSize, duration).SetEase(ease);
                it.DOScaleY(finalSizeY, duration).SetEase(ease);
            });
        }
        else
        {
            clone.GetComponent<RectTransform>().DOScale(finalSize, duration).SetEase(ease);
        }

        await UniTask.Delay(TimeSpan.FromSeconds(duration));

        Destroy(clone);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(PulseElement))]
public class PulseElementEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var component = (PulseElement) target;

        if (GUILayout.Button("Pulse"))
        {
            component.Pulse();
        }
    }
}
#endif
