using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DifficultyPillGroup : MonoBehaviour, ScreenBecameActiveListener, ScreenBecameInactiveListener
{

    public GameObject difficultyPillPrefab;

    private List<DifficultyPill> difficultyPills = new List<DifficultyPill>();

    public void OnScreenBecameActive()
    {
        if (Context.SelectedLevel == null) return;

        var meta = Context.SelectedLevel.Meta;
        var hasPreferredDifficulty = false;
        foreach (var section in meta.charts)
        {
            var difficultyPill = Instantiate(difficultyPillPrefab, transform).GetComponent<DifficultyPill>();
            difficultyPill.SetModel(section);
            difficultyPills.Add(difficultyPill);

            if (Context.PreferredDifficulty == difficultyPill.Difficulty)
            {
                hasPreferredDifficulty = true;
                difficultyPill.Select(false);
            }
        }

        if (!hasPreferredDifficulty)
        {
            if (Context.PreferredDifficulty == Difficulty.Extreme)
            {
                difficultyPills.Last().Select(false);
            }
            else
            {
                difficultyPills.First().Select(false);
            }
        }

        LayoutFixer.Fix(transform);
    }

    public void OnScreenBecameInactive()
    {
        difficultyPills.ForEach(it => Destroy(it.transform.parent.gameObject)); // Destroy parent because pulse wrapper
        difficultyPills.Clear();
    }
    
}