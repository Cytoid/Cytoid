using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CaretSelect : MonoBehaviour
{

    public Transform leftCaret;
    public Transform rightCaret;
    public Text labelText;
    public List<string> labels;
    public List<string> values;
    public int defaultIndex;

    public int SelectedIndex { get; private set; } = -1;
    public string SelectedValue => values[SelectedIndex];
    
    public CaretSelectEvent onSelect = new CaretSelectEvent();
        
    private void Start()
    {
        if (SelectedIndex < 0)
        {
            SelectedIndex = defaultIndex;
            labelText.text = defaultIndex <= labels.Count - 1 ? labels[defaultIndex] : "N/A";
        }

        var left = true;
        foreach (var caret in new List<Transform>{leftCaret, rightCaret})
        {
            var interactable = caret.gameObject.AddComponent<InteractableMonoBehavior>();
            interactable.onPointerClick.AddListener(left ? (UnityAction<PointerEventData>) (_ => SelectPrevious()) : _ => SelectNext());
            interactable.scaleToOnClick = 0.95f;
            interactable.scaleOnClick = true;
            left = false;
        }
    }
    
    public int GetIndex(string value)
    {
        return values.FindIndex(it => it == value);
    }

    public void SelectPrevious()
    {
        Select((SelectedIndex - 1).Mod(values.Count));
    }

    public void SelectNext()
    {
        Select((SelectedIndex + 1) % values.Count);
    }

    public void Select(int index, bool anim = true, bool notify = true)
    {
        if (index < 0 || index >= values.Count) throw new ArgumentException();
        SelectedIndex = index;
        labelText.text = labels[index];
        if (anim)
            DOTween.Sequence()
                .Append(labelText.transform.DOScale(0.9f, 0.2f).SetEase(Ease.OutCubic))
                .Append(labelText.transform.DOScale(1f, 0.2f).SetEase(Ease.OutCubic));
        if (notify) onSelect.Invoke(SelectedIndex, SelectedValue);
    }
    
    public void Select(string value, bool anim = true, bool notify = true)
    {
        var index = values.FindIndex(it => it == value);
        if (index == -1) index = 0; // Default to first value
        Select(index, anim, notify);
    }

}

public class CaretSelectEvent : UnityEvent<int, string>
{
}