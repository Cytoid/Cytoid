using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class ToggleRadioGroup : RadioGroup
{
    private static GameObject radioButtonPrefab;

    public List<string> labels;
    public List<string> values;

    public override void Initialize()
    {
        if (radioButtonPrefab == null)
        {
            radioButtonPrefab = Resources.Load<GameObject>("Prefabs/UI/Preference/ToggleRadioButton");
        }

        Assert.IsTrue(labels.Count == values.Count);
        for (var i = 0; i < labels.Count; i++)
        {
            var label = labels[i];
            var value = values[i];
            var child = Instantiate(radioButtonPrefab, transform, false);
            var toggleRadioButton = child.GetComponent<ToggleRadioButton>();
            toggleRadioButton.label.text = label;
            toggleRadioButton.value = value;
        }

        base.Initialize();
    }
}