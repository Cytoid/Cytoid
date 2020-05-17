using UnityEngine;
using UnityEngine.UI;

public class ToggleRadioButton : RadioButton
{

    public Sprite radioOnSprite;
    public Sprite radioOffSprite;

    [GetComponentInChildren] public Text label;
    [GetComponentInChildren] public Image image;
    [GetComponentInChildren] public PulseElement pulseElement;

    public override void Select(bool pulse = true)
    {
        base.Select(pulse);
        image.sprite = radioOnSprite;
        if (pulse) pulseElement.Pulse();
    }

    public override void Unselect()
    {
        base.Unselect();
        image.sprite = radioOffSprite;
    }
    
}