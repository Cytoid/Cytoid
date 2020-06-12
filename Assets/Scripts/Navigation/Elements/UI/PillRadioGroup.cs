using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class PillRadioGroup : RadioGroup
{
    public List<string> labels;
    public List<string> values;

    public override void Initialize()
    {
        // Clear existing
        OnDestroy();
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }

        Assert.IsTrue(labels.Count == values.Count);
        for (var i = 0; i < labels.Count; i++)
        {
            var label = labels[i];
            var value = values[i];
            var child = Instantiate(NavigationUiElementProvider.Instance.pillRadioButton, transform, false);
            // Set its parent (PulseElement) instead
            child.transform.parent.GetComponent<RectTransform>().SetWidth(labels.Count > 2 ? 128 : 192);
            var pillRadioButton = child.GetComponent<PillRadioButton>();
            pillRadioButton.label.text = label;
            pillRadioButton.value = value;
            RadioButtons.Add(pillRadioButton);
        }

        base.Initialize();
    }
}