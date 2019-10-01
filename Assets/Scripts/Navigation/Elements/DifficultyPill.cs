using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DifficultyPill : InteractableMonoBehavior
{
    [GetComponent] public CanvasGroup canvasGroup;
    [GetComponentInChildrenName("Background")] public GradientMeshEffect gradientMesh;
    [GetComponentInChildrenName("Name")] public Text name;
    [GetComponentInChildrenName("Level")] public Text level;
    public PulseElement pulseElement;
    public bool isStatic;

    public Game gameToAttach;
    
    private LevelMeta.ChartSection section;
    public Difficulty Difficulty { get; private set; }

    protected void Awake()
    {
        if (gameToAttach != null)
        {
            gameToAttach.onGameReadyToLoad.AddListener(_ =>
            {
                SetModel(gameToAttach.Level.Meta.GetChartSection(gameToAttach.Difficulty.Id));
            });
        }
    }

    public void SetModel(LevelMeta.ChartSection section)
    {
        this.section = section;
        Difficulty = Difficulty.Parse(section.type);
        
        gradientMesh.SetGradient(Difficulty.Gradient);
        name.text = section.name ?? Difficulty.Name;
        level.text = "LV." + Difficulty.ConvertToDisplayLevel(section.difficulty);
        
        LayoutFixer.Fix(transform);
    }

    private void Update()
    {
        if (isStatic) return;
        if (Difficulty != null && Context.SelectedDifficulty == Difficulty)
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
        Context.PreferredDifficulty = Difficulty;
        this.GetScreenParent<GamePreparationScreen>().UpdateRankings();
    }

    public void Select(bool pulse = true)
    {
        Context.SelectedDifficulty = Difficulty;
        if (pulse) pulseElement.Pulse();
    }
    
}