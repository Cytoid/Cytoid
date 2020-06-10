using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class ToggleRadioGroup : RadioGroup
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
            var child = Instantiate(NavigationUiElementProvider.Instance.toggleRadioButton, transform, false);
            var toggleRadioButton = child.GetComponent<ToggleRadioButton>();
            toggleRadioButton.label.text = label;
            toggleRadioButton.value = value;
            RadioButtons.Add(toggleRadioButton);
        }

        base.Initialize();
    }

}