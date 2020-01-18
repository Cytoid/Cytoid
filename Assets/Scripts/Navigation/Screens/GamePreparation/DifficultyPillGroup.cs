using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DifficultyPillGroup : MonoBehaviour, ScreenBecameActiveListener
{

    public GameObject difficultyPillPrefab;

    private List<DifficultyPill> difficultyPills = new List<DifficultyPill>();

    private void Awake()
    {
        Context.OnSelectedLevelChanged.AddListener(Load);
    }

    public void Load(Level level)
    {
        if (level == null) return;
        foreach (Transform child in transform) Destroy(child.gameObject);
        difficultyPills.Clear();

        var meta = level.Meta;
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

    public void OnScreenBecameActive()
    {
        Load(Context.SelectedLevel);
    }

}