using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public class SelectPreferenceElement : PreferenceElement
{
    [GetComponentInChildren] public CaretSelect caretSelect;

    public void SetContent(string title, string description,
        Func<string> getter, Action<string> setter,
        (string, string)[] labelsToValues)
    {
        caretSelect.labels = labelsToValues.Select(it => it.Item1).ToList();
        caretSelect.values = labelsToValues.Select(it => it.Item2).ToList();
        caretSelect.Select(getter());
        caretSelect.onSelect.AddListener((_, it) => setter(it));
        base.SetContent(title, description);
    }
    
    public void SetContent(string title, string description,
        Func<float> getter, Action<float> setter,
        (string, float)[] labelsToValues)
    {
        caretSelect.labels = labelsToValues.Select(it => it.Item1).ToList();
        caretSelect.values = labelsToValues.Select(it => it.Item2.ToString(CultureInfo.InvariantCulture)).ToList();
        caretSelect.Select(getter().ToString(CultureInfo.InvariantCulture));
        caretSelect.onSelect.AddListener((_, it) => setter(float.Parse(it)));
        base.SetContent(title, description);
    }
    
    public void SetContent(string title, string description,
        Func<int> getter, Action<int> setter,
        (string, int)[] labelsToValues)
    {
        caretSelect.labels = labelsToValues.Select(it => it.Item1).ToList();
        caretSelect.values = labelsToValues.Select(it => it.Item2.ToString(CultureInfo.InvariantCulture)).ToList();
        caretSelect.Select(getter().ToString(CultureInfo.InvariantCulture));
        caretSelect.onSelect.AddListener((_, it) => setter(int.Parse(it)));
        base.SetContent(title, description);
    }
}