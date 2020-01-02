using DG.Tweening;
using UniRx.Async;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ToggleRadioButton : RadioButton
{

    private static bool loadingSprites;
    public Sprite radioOnSprite;
    public Sprite radioOffSprite;

    private Image image;
    private PulseElement pulseElement;

    private void Awake()
    {
        image = GetComponent<Image>();
        pulseElement = GetComponent<PulseElement>();
    }

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