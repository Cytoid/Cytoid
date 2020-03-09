using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UniRx.Async;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class PulseElement : MonoBehaviour
{
    private static bool cloning;

    public float initialAlpha = 0.6f;
    public float finalSize = 2f;
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
        // Return if this is a cloned instance
        if (cloning)
        {
            isCloned = true;
            return;
        }

        // Create holder
        holder = new GameObject(gameObject.name + "_PulseWrapper");
        holder.transform.parent = transform.parent;
        holder.transform.SetZ(transform.position.z);
        
        holderRectTransform = holder.AddComponent<RectTransform>(); 
        rectTransform = GetComponent<RectTransform>();

        // Transfer rect transform properties
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

        // Set up this rect transform
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
        
        // Recalculate size delta
        var newHolderSizeDelta = holderRectTransform.sizeDelta;
        var rectSizeDelta = rectTransform.sizeDelta;
        newHolderSizeDelta.x += rectSizeDelta.x;
        newHolderSizeDelta.y += rectSizeDelta.y;
        holderRectTransform.sizeDelta = newHolderSizeDelta;
    }

    public void Pulse()
    {
        print("pulsed");
        print(StackTraceUtility.ExtractStackTrace());
        if (holder == null)
        {
            throw new InvalidOperationException("Pulse element not initialized yet");
        }

        // Create clone
        cloning = true;
        var clone = Instantiate(gameObject, holder.transform);
        clone.name = "Pulse";
        Destroy(clone.GetComponent<PulseElement>());
        Destroy(clone.GetComponent<ScheduledPulse>());
        cloning = false;

        if (overlay) clone.transform.SetAsLastSibling(); else clone.transform.SetAsFirstSibling();
        foreach (var component in clone.gameObject.GetComponentsInChildren<MonoBehaviour>())
        {
            if (componentsToDestroyAfterClone.Any(it => it.GetType() == component.GetType()))
            {
                print("Destroyed " + component.GetType());
                Destroy(component);
            }
            else if (typesToDestroyAfterClone.Any(it => it == component.GetType()))
            {
                print("Destroyed " + component.GetType());
                Destroy(component);
            }
        }

        // Pulse
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
        if (clone == null) return;
        
        var cloneRectTransform = clone.GetComponent<RectTransform>();

        // BUG: Fix your shitty UI system, Unity.
        // Rebuild layout until clone has the correct rect (same as the original)
        var equal = false;
        while (!equal)
        {
            clone.transform.RebuildLayout();
            equal = cloneRectTransform.rect == rectTransform.rect;
            await UniTask.Yield();
        }
        
        canvasGroup.alpha = initialAlpha;
        canvasGroup.DOFade(0, duration);
        clone.GetComponent<RectTransform>().DOScale(finalSize, duration).SetEase(ease);
        
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