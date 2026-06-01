using DG.Tweening;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class GameRenderer
{
    public Game Game { get; }

    public float OpacityMultiplier { get; set; } = 1f;

    private Image cover;
    private GameObject boundaryTop;
    private GameObject boundaryBottom;
    private SpriteRenderer boundaryTopSpriteRenderer;
    private SpriteRenderer boundaryBottomSpriteRenderer;
    private Animator boundaryTopAnimator;
    private Animator boundaryBottomAnimator;

    public const float BoundaryOpacity = 0.2f;

    public GameRenderer(Game game)
    {
        Game = game;
        game.onGameLoaded.AddListener(_ => Game.BeforeStartTasks.Add(OnGameBeforeStart()));
        game.onGameCompleted.AddListener(_ => OnGameCompleted());
        game.onGameBeforeExit.AddListener(_ => OnGameBeforeExit());
    }

    public async UniTask OnGameBeforeStart()
    {
        // Boundaries
        boundaryTop = GameObjectProvider.Instance.boundaryTop;
        boundaryBottom = GameObjectProvider.Instance.boundaryBottom;
        boundaryTopSpriteRenderer = boundaryTop.GetComponentInChildren<SpriteRenderer>();
        boundaryBottomSpriteRenderer = boundaryBottom.GetComponentInChildren<SpriteRenderer>();
        boundaryTopAnimator = boundaryTop.GetComponentInChildren<Animator>();
        boundaryBottomAnimator = boundaryBottom.GetComponentInChildren<Animator>();

        if (Game.Chart.DisplayBoundaries && Game.State.Mode != GameMode.GlobalCalibration)
        {
            this.ListOf(boundaryTopSpriteRenderer, boundaryBottomSpriteRenderer)
                .ForEach(it =>
                {
                    it.color = it.color.WithAlpha(0);
                    it.DOFade(BoundaryOpacity, 0.4f);
                });
            Game.onTopBoundaryBounded.AddListener(_ => boundaryTopAnimator.Play("BoundaryBound"));
            Game.onBottomBoundaryBounded.AddListener(_ => boundaryBottomAnimator.Play("BoundaryBound"));
        }
        else
        {
            this.ListOf(boundaryTopSpriteRenderer, boundaryBottomSpriteRenderer)
                .ForEach(it => it.enabled = false);
        }

        if (Game.State.Mode != GameMode.GlobalCalibration && Game.Chart.DisplayBackground)
        {
            // Cover
            cover = GameObjectProvider.Instance.cover;
            cover.color = Color.white.WithAlpha(0);
            var path = "file://" + Game.Level.Path + Game.Level.Meta.background.path;
            cover.sprite = await Context.AssetMemory.LoadAsset<Sprite>(path, AssetTag.GameCover);
            cover.FitSpriteAspectRatio();
            cover.DOFade(Context.Player.Settings.CoverOpacity, 0.8f);
        }
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

    public void OnGameBeforeExit()
    {
        if (Context.ScreenManager.ActiveScreen != null)
        {
            Context.ScreenManager.ActiveScreen.State = ScreenState.Inactive;
        }
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
        if (Game.State.IsStarted && Game.State.IsPlaying && !Game.State.IsCompleted)
        {
            // Update opacity
            boundaryTopSpriteRenderer.color =
                boundaryTopSpriteRenderer.color.WithAlpha(BoundaryOpacity * OpacityMultiplier);
            boundaryBottomSpriteRenderer.color =
                boundaryBottomSpriteRenderer.color.WithAlpha(BoundaryOpacity * OpacityMultiplier);
        }
    }
    
}
