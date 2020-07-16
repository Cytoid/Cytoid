using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class RadioGroup : MonoBehaviour
{
    protected List<RadioButton> RadioButtons = new List<RadioButton>();
    public string defaultValue;

    private string value;

    public string Value
    {
        get => value;
    }

    public RadioGroupSelectEvent onSelect = new RadioGroupSelectEvent();

    private RadioButton selected;

    public int Size => RadioButtons.Count;

    private void Awake()
    {
        Initialize();
    }

    public virtual async void Initialize()
    {
        if (RadioButtons.Count == 0) RadioButtons = GetComponentsInChildren<RadioButton>().ToList();
        RadioButtons.ForEach(it => it.radioGroup = this);
        value = defaultValue;

        if (!Context.FontManager.Loaded) await UniTask.WaitUntil(() => Context.FontManager.Loaded);
        
        RadioButtons.ForEach(it => it.Unselect());
        if (RadioButtons.Any(it => it.value == value))
        {
            selected = RadioButtons.First(it => it.value == value);
            selected.Select(false);
        }
    }

    public virtual void OnDestroy()
    {
        RadioButtons.ForEach(it => Destroy(it.gameObject));
        RadioButtons.Clear();
        selected = null;
    }

    public int GetIndex(string value)
    {
        return RadioButtons.FindIndex(it => it.value == value);
    }

    public bool IsSelected(RadioButton radioButton)
    {
        if (!RadioButtons.Contains(radioButton)) throw new ArgumentOutOfRangeException();
        return radioButton.value == value;
    }

    public void Select(string value, bool notify = true)
    {
        if (value == null) value = defaultValue;
        if (value == this.value) return;

        this.value = value;
        if (selected != null) selected.Unselect();
        selected = RadioButtons.FirstOrDefault(it => it.value == value);
        if (selected == default) selected = RadioButtons.First(); // Default to first value
        selected.Select(false);
        if (notify)
        {
            onSelect.Invoke(value);
        }
    }

    public void RevertToDefault(bool notify = false)
    {
        Select(defaultValue, notify);
    }

}

public class RadioGroupSelectEvent : UnityEvent<string>
{
}