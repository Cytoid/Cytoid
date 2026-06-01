using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PillRadioButton : RadioButton
{
    public float radius = 16;

    public Text label;
    public Image image;
    public ImageWithIndependentRoundedCorners roundedImage;
    public PulseElement pulseElement;

    public Color activeTextColor = Color.black;

    private void Awake()
    {
        image = GetComponent<Image>();
        roundedImage = GetComponent<ImageWithIndependentRoundedCorners>();
        pulseElement = GetComponent<PulseElement>();
        label = GetComponentInChildren<Text>();
        label.fontStyle = FontStyle.Normal;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        label.transform.DOScale(0.9f, 0.2f).SetEase(Ease.OutCubic);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        label.transform.DOScale(1f, 0.2f).SetEase(Ease.OutCubic);
    }

    public override void Select(bool pulse = true)
    {
        base.Select(pulse);
        if (pulse) pulseElement.Pulse();
        SetBorderWidth(0);
        label.font = Context.FontManager.BoldFont;
        label.DOColor(activeTextColor, 0.2f).SetEase(Ease.OutCubic);
    }

    public override void Unselect()
    {
        base.Unselect();
        SetBorderWidth(4);
        label.font = Context.FontManager.RegularFont;
        label.DOColor(Color.white, 0.2f).SetEase(Ease.OutCubic);
    }

    private void SetBorderWidth(float width)
    {
        if (roundedImage == null) return;
        roundedImage.borderWidth = width;
        if (roundedImage.material != null)
            roundedImage.material.SetFloat(Shader.PropertyToID("_BorderWidth"), width);
    }
}
