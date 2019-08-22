using System.Collections.Generic;
using System.Linq.Expressions;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LabelSelect : MonoBehaviour, ScreenBecameActiveListener
{
    
    public List<CanvasGroup> labels;
    public bool rememberIndex;

    public LabelSelectEvent onSelect = new LabelSelectEvent();
    
    public int SelectedIndex { get; private set; }

    private void Awake()
    {
        // Make sure game objects are active
        for (var index = 0; index < labels.Count; index++)
        {
            var it = labels[index];
            it.gameObject.SetActive(true);
            var interactable = it.gameObject.AddComponent<InteractableMonoBehavior>();
            var toIndex = index;
            interactable.onPointerClick.AddListener(pointerData => Select(toIndex));
        }
    }

    public virtual void Select(int newIndex)
    {
        SelectedIndex = newIndex;
        for (var i = 0; i < labels.Count; i++)
        {
            labels[i].GetComponentInChildren<Text>().fontStyle =
                i == SelectedIndex ? FontStyle.Bold : FontStyle.Normal;
            labels[i].DOFade(i == SelectedIndex ? 1 : 0.3f, 0.4f);
        }

        OnSelect(newIndex);
    }

    protected virtual void OnSelect(int newIndex)
    {
        onSelect.Invoke(newIndex, labels[newIndex]);
    }

    public void OnScreenBecameActive()
    {
        if (!rememberIndex) SelectedIndex = 0;
        Select(SelectedIndex);
    }
    
}

public class LabelSelectEvent : UnityEvent<int, CanvasGroup>
{
}