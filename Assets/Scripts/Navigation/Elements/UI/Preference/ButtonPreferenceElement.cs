using System;

public class ButtonPreferenceElement : PreferenceElement
{
    [GetComponentInChildren] public SoftButton softButton;

    public ButtonPreferenceElement SetContent(string title, string description, string label, Action onClick)
    {
        base.SetContent(title, description);
        softButton.Label = label;
        softButton.onPointerClick.AddListener(_ => onClick());
        return this;
    }
}