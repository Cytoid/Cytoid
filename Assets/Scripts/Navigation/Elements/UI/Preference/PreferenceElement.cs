using System;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class PreferenceElement : MonoBehaviour, ScreenBecameActiveListener
{

    [GetComponentInChildrenName("Name")] public Text title;
    [GetComponentInChildrenName("Description")] public Text description;

    public bool saveSettingsOnChange;

    public virtual PreferenceElement SetContent(string title, string description)
    {
        if (title != null && this.title != null) this.title.text = title;
        if (description != null && this.description != null) this.description.text = description;
        if (description == "" && this.description != null) this.description.gameObject.SetActive(false);
        RebuildLayout();
        return this;
    }

    public async void OnScreenBecameActive()
    {
        RebuildLayout();
    }

    private async void RebuildLayout()
    {
        LayoutStaticizer.Activate(transform);
        LayoutFixer.Fix(transform);
        await UniTask.DelayFrame(5);
        if (this == null || transform == null) return;
        LayoutStaticizer.Staticize(transform);
    }

    public PreferenceElement SaveSettingsOnChange()
    {
        saveSettingsOnChange = true;
        return this;
    }
    
    protected Action<T> Wrap<T>(Action<T> action)
    {
        return value =>
        {
            action(value);
            if (saveSettingsOnChange) Context.Player.SaveSettings();
        };
    }
    
}