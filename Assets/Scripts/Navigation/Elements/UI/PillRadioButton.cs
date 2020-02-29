using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

public class PillRadioButton : RadioButton
{
    public float radius = 16;

    [GetComponentInChildren] public Text label;
    [GetComponent] public ProceduralImage image;
    [GetComponent] public PulseElement pulseElement;

    public Color activeTextColor = Color.black;

    private void Awake()
    {
        image = GetComponent<ProceduralImage>();
        pulseElement = GetComponent<PulseElement>();
        label.fontStyle = FontStyle.Normal;
    }

    private void Start()
    {
        var modifier = GetComponent<FreeModifier>();
        if (Index == 0)
        {
            modifier.Radius = new Vector4(radius, 0, 0, radius);
        }
        else if (Index == radioGroup.Size - 1)
        {
            modifier.Radius = new Vector4(0, radius, radius, 0);
        }
        else
        {
            modifier.Radius = new Vector4(0, 0, 0, 0);
        }
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
        image.BorderWidth = 0;
        label.font = Context.FontManager.BoldFont;
        label.DOColor(activeTextColor, 0.2f).SetEase(Ease.OutCubic);
    }

    public override void Unselect()
    {
        base.Unselect();
        image.BorderWidth = 2;
        label.font = Context.FontManager.RegularFont;
        label.DOColor(Color.white, 0.2f).SetEase(Ease.OutCubic);
    }
}