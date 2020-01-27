using System;
using System.Collections.Generic;
using System.Linq;

public class PillRadioGroupPreferenceElement : PreferenceElement
{
    [GetComponentInChildren] public PillRadioGroup radioGroup;
    
    public void SetContent(string title, string description,
        Func<string> getter, Action<string> setter,
        (string, string)[] labelsToValues)
    {
        radioGroup.labels = labelsToValues.Select(it => it.Item1).ToList();
        radioGroup.values = labelsToValues.Select(it => it.Item2).ToList();
        radioGroup.defaultValue = getter();
        radioGroup.Initialize();
        radioGroup.onSelect.AddListener(it => setter(it));
        base.SetContent(title, description);
    }
    
    public void SetContent(string title, string description,
        Func<string> getter, Action<string> setter)
    {
        var labelsToValues = new List<(string, bool)> {("Off", false), ("On", true)};
        radioGroup.labels = labelsToValues.Select(it => it.Item1).ToList();
        radioGroup.values = labelsToValues.Select(it => it.Item2.ToString()).ToList();
        radioGroup.defaultValue = getter();
        radioGroup.Initialize();
        radioGroup.onSelect.AddListener(it => setter(it));
        base.SetContent(title, description);
    }
    
    public void SetContent(string title, string description,
        Func<int> getter, Action<int> setter,
        (string, int)[] labelsToValues)
    {
        radioGroup.labels = labelsToValues.Select(it => it.Item1).ToList();
        radioGroup.values = labelsToValues.Select(it => it.Item2.ToString()).ToList();
        radioGroup.defaultValue = getter().ToString();
        radioGroup.Initialize();
        radioGroup.onSelect.AddListener(it => setter(int.Parse(it)));
        base.SetContent(title, description);
    }
    
    public void SetContent(string title, string description,
        Func<bool> getter, Action<bool> setter,
        (string, string)[] labelsToValues)
    {
        radioGroup.labels = labelsToValues.Select(it => it.Item1).ToList();
        radioGroup.values = labelsToValues.Select(it => it.Item2).ToList();
        radioGroup.defaultValue = getter().ToString();
        radioGroup.Initialize();
        radioGroup.onSelect.AddListener(it => setter(bool.Parse(it)));
        base.SetContent(title, description);
    }
    
    public void SetContent(string title, string description,
        Func<bool> getter, Action<bool> setter)
    {
        var labelsToValues = new List<(string, bool)> {("Off", false), ("On", true)};
        radioGroup.labels = labelsToValues.Select(it => it.Item1).ToList();
        radioGroup.values = labelsToValues.Select(it => it.Item2.ToString()).ToList();
        radioGroup.defaultValue = getter().ToString();
        radioGroup.Initialize();
        radioGroup.onSelect.AddListener(it => setter(bool.Parse(it)));
        base.SetContent(title, description);
    }

    
}