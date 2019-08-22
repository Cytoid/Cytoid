using DG.Tweening;
using UniRx.Async;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ToggleRadioButton : RadioButton
{

    private static bool loadingSprites;
    public static Sprite RadioOnSprite;
    public static Sprite RadioOffSprite;

    private Image image;
    private PulseElement pulseElement;

    private async void Awake()
    {
        image = GetComponent<Image>();
        pulseElement = GetComponent<PulseElement>();
        if (loadingSprites) await UniTask.WaitUntil(() => !loadingSprites);
        if (RadioOnSprite == null)
        {
            loadingSprites = true;
            RadioOnSprite = Resources.Load<Sprite>("Sprites/Icons/RadioOn");
            RadioOffSprite = Resources.Load<Sprite>("Sprites/Icons/RadioOff");
            loadingSprites = false;
        }
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        transform.DOScale(0.9f, 0.2f).SetEase(Ease.OutCubic);
    }
        
    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        transform.DOScale(1f, 0.2f).SetEase(Ease.OutCubic);
    }

    public override void Select(bool pulse = true)
    {
        base.Select(pulse);
        image.sprite = RadioOnSprite;
        if (pulse) pulseElement.Pulse();
    }

    public override void Unselect()
    {
        base.Unselect();
        image.sprite = RadioOffSprite;
    }
    
}