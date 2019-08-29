using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ListContainer<T, TE> : MonoBehaviour where TE : ContainerEntry<T>
{
    [GetComponent] public CanvasGroup canvasGroup;
    public GameObject entryPrefab;
    public bool resetVerticalScrollPosition = true;
    
    protected List<TE> entries = new List<TE>();

    public virtual void SetData(IEnumerable<T> data)
    {
        Clear();
        canvasGroup.alpha = 0;
        foreach (var datum in data)
        {
            var entryElement = Instantiate(entryPrefab, transform).GetComponent<TE>();
            entries.Add(entryElement);
            entryElement.SetModel(datum);
            LayoutFixer.Fix(entryElement.transform);
        }
        LayoutFixer.Fix(transform);
        canvasGroup.DOFade(1, 0.4f).SetDelay(0.1f).SetEase(Ease.OutCubic);
        
        if (resetVerticalScrollPosition)
        {
            var rect = (RectTransform) transform;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 0);
        }
    }

    public virtual void Clear()
    {
        entries.ForEach(it => Destroy(it.gameObject));
        entries.Clear();
    }
}