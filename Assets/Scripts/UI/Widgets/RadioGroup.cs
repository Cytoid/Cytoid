using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RadioGroup : MonoBehaviour
{

    private List<RadioButton> radioButtons;
    public string defaultValue;

    private string value;
    public string Value
    {
        get => value;
        set
        {
            if (value == this.value) return;
            Selected.Unselect(); 
            this.value = value;
            foreach (var listener in radioGroupChangeListeners) listener.OnRadioGroupChange(this, value);
        }
    }

    protected HashSet<RadioGroupChangeListener> radioGroupChangeListeners = new HashSet<RadioGroupChangeListener>();

    public RadioButton Selected => radioButtons.Find(it => it.value == value);

    public int Size => radioButtons.Count;
    
    private void Awake()
    {
        radioButtons = GetComponentsInChildren<RadioButton>().ToList();
        radioButtons.ForEach(it => it.radioGroup = this);
        value = defaultValue;
    }

    private void Start()
    {
        radioButtons.First(it => it.value == value).Select(false);
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
    
    public void AddHandler(RadioGroupChangeListener listener)
    {
        radioGroupChangeListeners.Add(listener);
    }

    public void RemoveHandler(RadioGroupChangeListener listener)
    {
        radioGroupChangeListeners.Remove(listener);
    }

}

public interface RadioGroupChangeListener
{

    void OnRadioGroupChange(RadioGroup radioGroup, string value);

}