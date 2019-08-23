using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class LeaderboardElement : MonoBehaviour
{
    [GetComponent] public CanvasGroup canvasGroup;
    public GameObject entryPrefab;
    
    private List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

    public void SetModel(IEnumerable<Leaderboard.Entry> data)
    {
        Clear();
        canvasGroup.alpha = 0;
        foreach (var datum in data)
        {
            var entryElement = Instantiate(entryPrefab, transform).GetComponent<LeaderboardEntry>();
            entries.Add(entryElement);
            entryElement.SetModel(datum);
            LayoutFixer.Fix(entryElement.transform);
        }
        LayoutFixer.Fix(transform);
        canvasGroup.DOFade(1, 0.4f).SetEase(Ease.OutCubic);
    }

    public void Clear()
    {
        entries.ForEach(it => Destroy(it.gameObject));
        entries.Clear();
    }
}