using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class PillRadioGroup : RadioGroup
{
    private static GameObject radioButtonPrefab;

    public List<string> labels;
    public List<string> values;

    public override void Initialize()
    {
        if (radioButtonPrefab == null)
        {
            radioButtonPrefab = Resources.Load<GameObject>("Prefabs/UI/Preference/PillRadioButton");
        }

        Assert.IsTrue(labels.Count == values.Count);
        radioButtonPrefab.GetComponent<RectTransform>().SetWidth(labels.Count > 2 ? 128 : 192);
        for (var i = 0; i < labels.Count; i++)
        {
            var label = labels[i];
            var value = values[i];
            var child = Instantiate(radioButtonPrefab, transform, false);
            var pillRadioButton = child.GetComponent<PillRadioButton>();
            pillRadioButton.label.text = label;
            pillRadioButton.value = value;
        }

        base.Initialize();
    }
}