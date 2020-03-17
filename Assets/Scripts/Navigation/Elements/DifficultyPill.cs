using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DifficultyPill : InteractableMonoBehavior, ScreenBecameActiveListener
{
    [GetComponent] public CanvasGroup canvasGroup;

    [GetComponentInChildrenName("Background")]
    public GradientMeshEffect gradientMesh;

    [GetComponentInChildrenName("Name")] public Text name;
    [GetComponentInChildrenName("Level")] public Text level;
    public PulseElement pulseElement;
    public bool isStatic;

    public Game gameToAttach;
    public bool attachToContext;

    private LevelMeta.ChartSection section;
    public Difficulty Difficulty { get; private set; }

    protected void Awake()
    {
        gradientMesh.SetGradient(new ColorGradient(Color.clear, Color.clear, 0));
        name.text = "";
        level.text = "";
        if (gameToAttach != null)
        {
            isStatic = true;
            gameToAttach.onGameLoaded.AddListener(_ =>
            {
                SetModel(gameToAttach.Level.Meta.GetChartSection(gameToAttach.Difficulty.Id));
            });
        }
    }

    public void OnScreenBecameActive()
    {
        if (attachToContext)
        {
            isStatic = true;
            SetModel(Context.SelectedLevel.Meta.GetChartSection(Context.SelectedDifficulty.Id));
        }
    }

    public void SetModel(LevelMeta.ChartSection section)
    {
        this.section = section;
        Difficulty = Difficulty.Parse(section.type);

        gradientMesh.SetGradient(Difficulty.Gradient);
        name.text = !section.name.IsNullOrEmptyTrimmed() ? section.name : Difficulty.Name;
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
        var willUpdateScreenInfo = Context.PreferredDifficulty != Difficulty;
        Select();
        Context.PreferredDifficulty = Difficulty;
        if (willUpdateScreenInfo)
        {
            this.GetScreenParent<GamePreparationScreen>().Apply(it =>
            {
                it.LoadLevelPerformance();
                it.rankingsTab.UpdateRankings(it.Level.Id, Context.SelectedDifficulty.Id);
            });
        }
    }

    public void Select(bool pulse = true)
    {
        Context.SelectedDifficulty = Difficulty;
        if (pulse) pulseElement.Pulse();
    }
}