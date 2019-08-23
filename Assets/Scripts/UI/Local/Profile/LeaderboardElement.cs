using System.Collections.Generic;
using UnityEngine;

public class LeaderboardElement : MonoBehaviour
{
    public GameObject entryPrefab;
    
    private List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

    public void SetModel(IEnumerable<Leaderboard.Entry> data)
    {
        entries.ForEach(it => Destroy(it.gameObject));
        entries.Clear();
        foreach (var datum in data)
        {
            var entryElement = Instantiate(entryPrefab, transform).GetComponent<LeaderboardEntry>();
            entries.Add(entryElement);
            entryElement.SetModel(datum);
            LayoutFixer.Fix(entryElement.transform);
        }
        LayoutFixer.Fix(transform);
    }
}