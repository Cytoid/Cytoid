using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class RadioGroup : MonoBehaviour
{
    public bool debug = false;

    private List<RadioButton> radioButtons;
    public string defaultValue;

    private string value;

    public string Value
    {
        get => value;
    }

    public RadioGroupSelectEvent onSelect = new RadioGroupSelectEvent();

    private RadioButton selected;

    public int Size => radioButtons.Count;

    private void Awake()
    {
        radioButtons = GetComponentsInChildren<RadioButton>().ToList();
        radioButtons.ForEach(it => it.radioGroup = this);
        value = defaultValue;
    }

    private void Start()
    {
        radioButtons.ForEach(it => it.Unselect());
        selected = radioButtons.First(it => it.value == value);
        selected.Select(false);
    }

    public int GetIndex(string value)
    {
        return radioButtons.FindIndex(it => it.value == value);
    }

    public bool IsSelected(RadioButton radioButton)
    {
        if (!radioButtons.Contains(radioButton)) throw new ArgumentOutOfRangeException();
        return radioButton.value == value;
    }

    public void Select(string value, bool notify = true)
    {
        if (value == this.value) return;
        if (debug)
        {
            print("setting value to " + value);
        }

        this.value = value;
        if (selected != null) selected.Unselect();
        selected = radioButtons.First(it => it.value == value);
        selected.Select(false);
        if (notify)
        {
            onSelect.Invoke(value);
        }
    }
}

public class RadioGroupSelectEvent : UnityEvent<string>
{
}