using DG.Tweening;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GameRenderer
{
    public Game Game { get; }

    private Image cover;
    private GameObject boundaryTop;
    private GameObject boundaryBottom;
    private Animator boundaryTopAnimator;
    private Animator boundaryBottomAnimator;

    public float boundaryOpacity = 0.2f;

    public GameRenderer(Game game)
    {
        Game = game;
        game.onGameLoaded.AddListener(_ => Game.BeforeStartTasks.Add(OnGameBeforeStart()));
        game.onGameCompleted.AddListener(_ => OnGameCompleted());
        game.onGameReadyToExit.AddListener(_ => OnGameReadyToExit());
    }

    public async UniTask OnGameBeforeStart()
    {
        // Boundaries
        boundaryTop = GameObjectProvider.Instance.boundaryTop;
        boundaryBottom = GameObjectProvider.Instance.boundaryBottom;
        boundaryTopAnimator = boundaryTop.GetComponentInChildren<Animator>();
        boundaryBottomAnimator = boundaryBottom.GetComponentInChildren<Animator>();
        if (!Context.LocalPlayer.Settings.DisplayBoundaries)
        {
            this.ListOf(
                boundaryTop.GetComponentInChildren<SpriteRenderer>(),
                boundaryBottom.GetComponentInChildren<SpriteRenderer>()
            ).ForEach(it => it.enabled = false);
        }
        else
        {
            this.ListOf(
                boundaryTop.GetComponentInChildren<SpriteRenderer>(),
                boundaryBottom.GetComponentInChildren<SpriteRenderer>()
            ).ForEach(it =>
            {
                it.color = it.color.WithAlpha(0);
                it.DOFade(boundaryOpacity, 0.4f);
            });
            Game.onTopBoundaryBounded.AddListener(_ => boundaryTopAnimator.Play("BoundaryBound"));
            Game.onBottomBoundaryBounded.AddListener(_ => boundaryBottomAnimator.Play("BoundaryBound"));
        }
        
        // Cover
        cover = GameObjectProvider.Instance.cover;
        cover.color = Color.white.WithAlpha(0);
        var path = "file://" + Game.Level.Path + Game.Level.Meta.background.path;
        cover.sprite = await Context.AssetMemory.LoadAsset<Sprite>(path, AssetTag.GameCover);
        cover.FitSpriteAspectRatio();
        cover.DOFade(Context.LocalPlayer.Settings.CoverOpacity, 0.8f);
    }

    public void OnGameCompleted()
    {
        this.ListOf(
            boundaryTop.GetComponentInChildren<SpriteRenderer>(),
            boundaryBottom.GetComponentInChildren<SpriteRenderer>()
        ).ForEach(it =>
        {
            it.DOFade(0, 0.4f);
        });
    }

    public void OnGameReadyToExit()
    {
        Context.ScreenManager.ActiveScreen.State = ScreenState.Inactive;
        cover.DOFade(0, 0.8f);
    }

    public void OnUpdate()
    {
        if (!Game.IsLoaded) return;
        var chart = Game.Chart;
        // Boundaries
        if (chart.CurrentPageId < chart.Model.page_list.Count)
        {
            boundaryTopAnimator.speed =
                chart.Model.tempo_list[0].value / 1000000f /
                (chart.Model.page_list[chart.CurrentPageId].end_time -
                 chart.Model.page_list[chart.CurrentPageId].start_time);
            boundaryBottomAnimator.speed = boundaryTopAnimator.speed;
        }
        else
        {
            boundaryTopAnimator.speed = 1;
            boundaryBottomAnimator.speed = boundaryTopAnimator.speed;
        }
        boundaryTop.transform.position = new Vector3(0, chart.GetBoundaryPosition(false), 0);
        boundaryBottom.transform.position = new Vector3(0, chart.GetBoundaryPosition(true), 0);
    }
    
}