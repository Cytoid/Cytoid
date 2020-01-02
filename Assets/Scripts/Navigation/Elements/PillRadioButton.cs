using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

public class PillRadioButton : RadioButton
{
    public bool debug = false;
    public float radius = 16;

    [GetComponent] public ProceduralImage image;
    [GetComponent] public PulseElement pulseElement;
    [GetComponentInChildren] public Text text;

    private void Awake()
    {
        image = GetComponent<ProceduralImage>();
        pulseElement = GetComponent<PulseElement>();
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
            modifier.Radius = new Vector4(radius, radius, radius, radius);
        }
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        text.transform.DOScale(0.9f, 0.2f).SetEase(Ease.OutCubic);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        text.transform.DOScale(1f, 0.2f).SetEase(Ease.OutCubic);
    }

    public override void Select(bool pulse = true)
    {
        base.Select(pulse);
        if (debug) print("Selected index " + Index);
        if (pulse) pulseElement.Pulse();
        image.BorderWidth = 0;
        text.fontStyle = FontStyle.Bold;
        text.DOColor(Color.black, 0.2f).SetEase(Ease.OutCubic);
    }

    public override void Unselect()
    {
        base.Unselect();
        if (debug) print("Unselected index " + Index);
        image.BorderWidth = 2;
        text.fontStyle = FontStyle.Normal;
        text.DOColor(Color.white, 0.2f).SetEase(Ease.OutCubic);
    }
}