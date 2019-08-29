using UnityEngine;

public class GameRenderer
{
    public Game Game { get; }

    private GameObject boundaryTop;
    private GameObject boundaryBottom;
    private Animator boundaryBottomAnimator;
    private Animator boundaryTopAnimator;

    public GameRenderer(Game game)
    {
        Game = game;
        game.onGameLoaded.AddListener(OnGameLoaded);
        game.onGameUpdate.AddListener(OnGameUpdate);
        game.onTopBoundaryBounded.AddListener(_ => boundaryTopAnimator.Play("Boundary_Bound"));
        game.onBottomBoundaryBounded.AddListener(_ => boundaryBottomAnimator.Play("Boundary_Bound"));
    }

    public void OnGameLoaded(Game game)
    {
        // Boundaries
        boundaryTop = GameObjectProvider.Instance.boundaryTop;
        boundaryBottom = GameObjectProvider.Instance.boundaryBottom;
        boundaryTopAnimator = boundaryTop.GetComponentInChildren<Animator>();
        boundaryBottomAnimator = boundaryBottom.GetComponentInChildren<Animator>();
        if (!Context.LocalPlayer.ShowBoundaries)
        {
            boundaryTop.GetComponentInChildren<SpriteRenderer>().enabled = false;
            boundaryBottom.GetComponentInChildren<SpriteRenderer>().enabled = false;
        }
    }

    public void OnGameUpdate(Game game)
    {
        var chart = game.Chart;
        
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