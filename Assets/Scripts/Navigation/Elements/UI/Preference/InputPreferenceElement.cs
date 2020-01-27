using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class InputPreferenceElement : PreferenceElement
{
    [GetComponentInChildren] public InputField inputField;

    [GetComponentInChildrenName("Unit")] public Text unit;
    [GetComponentInChildrenName("Placeholder")] public Text placeholder;
    
    public void SetContent(string title, string description,
        Func<string> getter, Action<string> setter, string unit, string placeholder, bool widerInput = false)
    {
        if (unit != null) this.unit.text = unit;
        if (widerInput) inputField.GetComponent<RectTransform>().SetWidth(198);
        
        if (placeholder != null) this.placeholder.text = placeholder;
        inputField.SetTextWithoutNotify(getter());
        inputField.onEndEdit.AddListener(it => setter(it));
        base.SetContent(title, description);
    }
    
    public void SetContent(string title, string description,
        Func<float> getter, Action<float> setter, string unit, string placeholder, bool widerInput = false)
    {
        if (unit != null) this.unit.text = unit;
        if (widerInput) inputField.GetComponent<RectTransform>().SetWidth(198);
        
        if (placeholder != null) this.placeholder.text = placeholder;
        inputField.contentType = InputField.ContentType.DecimalNumber;
        inputField.SetTextWithoutNotify(getter().ToString(CultureInfo.InvariantCulture));
        inputField.onEndEdit.AddListener(FloatSettingHandler(inputField, getter, setter));
        base.SetContent(title, description);
    }
    
    public void SetContent(string title, string description,
        Func<Color> getter, Action<Color> setter, string unit, string placeholder, bool widerInput = false)
    {
        if (unit != null) this.unit.text = unit;
        if (widerInput) inputField.GetComponent<RectTransform>().SetWidth(198);
        
        if (placeholder != null) this.placeholder.text = placeholder;
        inputField.SetTextWithoutNotify(getter().ColorToString());
        inputField.onEndEdit.AddListener(ColorSettingHandler(inputField, getter, setter));
        base.SetContent(title, description);
    }
    
    private static UnityAction<string> FloatSettingHandler(InputField inputField, Func<float> defaultValueGetter,
        Action<float> setter)
    {
        return it =>
        {
            if (float.TryParse(it, out var value))
            {
                setter(value);
            }
            else
            {
                inputField.text = defaultValueGetter().ToString(CultureInfo.InvariantCulture);
            }
        };
    }

    private static UnityAction<string> ColorSettingHandler(InputField inputField, Func<Color> defaultValueGetter,
        Action<Color> setter)
    {
        return it =>
        {
            var value = it.ToColor();
            if (value != Color.clear)
            {
                setter(value);
            }
            else
            {
                inputField.text = defaultValueGetter().ColorToString();
            }
        };
    }
}