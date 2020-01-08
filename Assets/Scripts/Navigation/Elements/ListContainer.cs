using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ListContainer<T, TE> : MonoBehaviour where TE : ContainerEntry<T>
{
    [GetComponent] public CanvasGroup canvasGroup;
    public GameObject entryPrefab;
    
    public List<TE> Entries { get; }= new List<TE>();

    public virtual void SetData(IEnumerable<T> data)
    {
        Clear();
        canvasGroup.alpha = 0;
        foreach (var datum in data)
        {
            var entryElement = Instantiate(entryPrefab, transform).GetComponent<TE>();
            Entries.Add(entryElement);
            entryElement.SetModel(datum);
            LayoutFixer.Fix(entryElement.transform);
        }
        LayoutFixer.Fix(transform);
        canvasGroup.DOFade(1, 0.4f).SetDelay(0.1f).SetEase(Ease.OutCubic);
    }

    public virtual void Clear()
    {
        Entries.ForEach(it => Destroy(it.gameObject));
        Entries.Clear();
    }
}