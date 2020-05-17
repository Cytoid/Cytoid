using System;
using System.Collections.Generic;
using System.Linq;

public class PillRadioGroupPreferenceElement : PreferenceElement
{
    [GetComponentInChildren] public PillRadioGroup radioGroup;
    
    public PillRadioGroupPreferenceElement SetContent(string title, string description,
        Func<string> getter, Action<string> setter,
        (string, string)[] labelsToValues)
    {
        radioGroup.labels = labelsToValues.Select(it => it.Item1).ToList();
        radioGroup.values = labelsToValues.Select(it => it.Item2).ToList();
        radioGroup.defaultValue = getter();
        radioGroup.Initialize();
        radioGroup.onSelect.AddListener(it => Wrap(setter)(it));
        base.SetContent(title, description);
        return this;
    }
    
    public PillRadioGroupPreferenceElement SetContent(string title, string description,
        Func<string> getter, Action<string> setter)
    {
        var labelsToValues = new List<(string, bool)> {("SETTINGS_OFF".Get(), false), ("SETTINGS_ON".Get(), true)};
        radioGroup.labels = labelsToValues.Select(it => it.Item1).ToList();
        radioGroup.values = labelsToValues.Select(it => it.Item2.ToString()).ToList();
        radioGroup.defaultValue = getter();
        radioGroup.Initialize();
        radioGroup.onSelect.AddListener(it => Wrap(setter)(it));
        base.SetContent(title, description);
        return this;
    }
    
    public PillRadioGroupPreferenceElement SetContent(string title, string description,
        Func<int> getter, Action<int> setter,
        (string, int)[] labelsToValues)
    {
        radioGroup.labels = labelsToValues.Select(it => it.Item1).ToList();
        radioGroup.values = labelsToValues.Select(it => it.Item2.ToString()).ToList();
        radioGroup.defaultValue = getter().ToString();
        radioGroup.Initialize();
        radioGroup.onSelect.AddListener(it => Wrap(setter)(int.Parse(it)));
        base.SetContent(title, description);
        return this;
    }
    
    public PillRadioGroupPreferenceElement SetContent(string title, string description,
        Func<bool> getter, Action<bool> setter,
        (string, string)[] labelsToValues)
    {
        radioGroup.labels = labelsToValues.Select(it => it.Item1).ToList();
        radioGroup.values = labelsToValues.Select(it => it.Item2).ToList();
        radioGroup.defaultValue = getter().ToString();
        radioGroup.Initialize();
        radioGroup.onSelect.AddListener(it => Wrap(setter)(bool.Parse(it)));
        base.SetContent(title, description);
        return this;
    }
    
    public PillRadioGroupPreferenceElement SetContent(string title, string description,
        Func<bool> getter, Action<bool> setter)
    {
        var labelsToValues = new List<(string, bool)> {("SETTINGS_OFF".Get(), false), ("SETTINGS_ON".Get(), true)};
        radioGroup.labels = labelsToValues.Select(it => it.Item1).ToList();
        radioGroup.values = labelsToValues.Select(it => it.Item2.ToString()).ToList();
        radioGroup.defaultValue = getter().ToString();
        radioGroup.Initialize();
        radioGroup.onSelect.AddListener(it => Wrap(setter)(bool.Parse(it)));
        base.SetContent(title, description);
        return this;
    }

    
}