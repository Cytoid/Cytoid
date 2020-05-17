using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public class SelectPreferenceElement : PreferenceElement
{
    [GetComponentInChildren] public CaretSelect caretSelect;

    public SelectPreferenceElement SetContent(string title, string description,
        Func<string> getter, Action<string> setter,
        (string, string)[] labelsToValues)
    {
        caretSelect.labels = labelsToValues.Select(it => it.Item1).ToList();
        caretSelect.values = labelsToValues.Select(it => it.Item2).ToList();
        caretSelect.Select(getter());
        caretSelect.onSelect.AddListener((_, it) => Wrap(setter)(it));
        base.SetContent(title, description);
        return this;
    }
    
    public SelectPreferenceElement SetContent(string title, string description,
        Func<float> getter, Action<float> setter,
        (string, float)[] labelsToValues)
    {
        caretSelect.labels = labelsToValues.Select(it => it.Item1).ToList();
        caretSelect.values = labelsToValues.Select(it => it.Item2.ToString(CultureInfo.InvariantCulture)).ToList();
        caretSelect.Select(getter().ToString(CultureInfo.InvariantCulture));
        caretSelect.onSelect.AddListener((_, it) => Wrap(setter)(float.Parse(it)));
        base.SetContent(title, description);
        return this;
    }
    
    public SelectPreferenceElement SetContent(string title, string description,
        Func<int> getter, Action<int> setter,
        (string, int)[] labelsToValues)
    {
        caretSelect.labels = labelsToValues.Select(it => it.Item1).ToList();
        caretSelect.values = labelsToValues.Select(it => it.Item2.ToString(CultureInfo.InvariantCulture)).ToList();
        caretSelect.Select(getter().ToString(CultureInfo.InvariantCulture));
        caretSelect.onSelect.AddListener((_, it) => Wrap(setter)(int.Parse(it)));
        base.SetContent(title, description);
        return this;
    }
    
    public SelectPreferenceElement SetContent<TEnum>(string title, string description,
        Func<TEnum> getter, Action<TEnum> setter,
        (string, TEnum)[] labelsToValues) where TEnum : Enum
    {
        caretSelect.labels = labelsToValues.Select(it => it.Item1).ToList();
        caretSelect.values = labelsToValues.Select(it => it.Item2.ToString()).ToList();
        caretSelect.Select(Enum.GetName(typeof(TEnum), getter()).ToString(CultureInfo.InvariantCulture));
        caretSelect.onSelect.AddListener((_, it) => Wrap(setter)((TEnum) Enum.Parse(typeof(TEnum), it)));
        base.SetContent(title, description);
        return this;
    }
}