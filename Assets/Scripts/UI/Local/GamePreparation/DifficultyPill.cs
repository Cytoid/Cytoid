using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DifficultyPill : InteractableMonoBehavior
{
    [GetComponent] public PulseElement pulseElement;
    [GetComponent] public CanvasGroup canvasGroup;
    [GetComponentInChildrenName("Background")] public GradientMeshEffect gradientMesh;
    [GetComponentInChildrenName("Name")] public Text name;
    [GetComponentInChildrenName("Level")] public Text level;

    private LevelMeta.ChartSection section;
    public Difficulty Difficulty { get; private set; }
    
    public void SetModel(LevelMeta.ChartSection section)
    {
        this.section = section;
        Difficulty = Difficulty.Parse(section.type);
        
        gradientMesh.SetGradient(Difficulty.Gradient);
        name.text = section.name ?? Difficulty.Name;
        level.text = "LV." + Difficulty.ConvertToDisplayLevel(section.difficulty);
    }

    private void Update()
    {
        if (Difficulty != null && Context.SelectedDifficulty == Difficulty)
        {
            canvasGroup.DOFade(1, 0.2f);
        }
        else
        {
            canvasGroup.DOFade(0.5f, 0.2f);
        }
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        Select();
        Context.PreferredDifficulty = Difficulty;
        this.GetOwningScreen<GamePreparationScreen>().UpdateRankings();
    }

    public void Select(bool pulse = true)
    {
        Context.SelectedDifficulty = Difficulty;
        if (pulse) pulseElement.Pulse();
    }
    
}