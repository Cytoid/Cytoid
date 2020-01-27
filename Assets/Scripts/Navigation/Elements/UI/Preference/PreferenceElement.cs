using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class PreferenceElement : MonoBehaviour, ScreenBecameActiveListener
{

    [GetComponentInChildrenName("Name")] public Text title;
    [GetComponentInChildrenName("Description")] public Text description;

    public virtual void SetContent(string title, string description)
    {
        if (title != null && this.title != null) this.title.text = title;
        if (description != null && this.description != null) this.description.text = description;
        if (description == "" && this.description != null) this.description.gameObject.SetActive(false);
        transform.RebuildLayout();
    }

    public async void OnScreenBecameActive()
    {
        LayoutStaticizer.Activate(transform);
        LayoutFixer.Fix(transform);
        await UniTask.DelayFrame(5);
        LayoutStaticizer.Staticize(transform);
    }
}