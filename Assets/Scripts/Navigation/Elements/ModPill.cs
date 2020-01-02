using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class ModPill : InteractableMonoBehavior
{
    [GetComponent] public CanvasGroup canvasGroup;
    public PulseElement pulseElement;
    public Mod mod;
    public List<Mod> modsToDisable;
    public bool isStatic;

    private void Update()
    {
        if (isStatic) return;
        if (Context.SelectedMods.Contains(mod))
        {
            if (canvasGroup.alpha < 1) canvasGroup.DOFade(1, 0.2f);
        }
        else
        {
            if (canvasGroup.alpha > 0.5f) canvasGroup.DOFade(0.5f, 0.2f);
        }
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        if (isStatic) return;
        Select();
    }

    public void Select(bool pulse = true)
    {
        if (Context.SelectedMods.Contains(mod))
        {
            Context.SelectedMods.Remove(mod);
        }
        else
        {
            Context.SelectedMods.Add(mod);
            modsToDisable.ForEach(it => Context.SelectedMods.Remove(it));
        }

        if (pulse) pulseElement.Pulse();
        
        // Save config
        Context.LocalPlayer.EnabledMods = Context.SelectedMods.ToList();
    }
    
}