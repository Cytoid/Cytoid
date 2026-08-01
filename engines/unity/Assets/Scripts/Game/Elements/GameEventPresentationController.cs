using UnityEngine;

/// <summary>
/// Applies the seek-safe C2 event presentation after storyboard updates for the frame.
/// Storyboard scanline color remains the final override inside Scanner.
/// </summary>
public sealed class GameEventPresentationController
{
    private readonly Game game;
    private readonly ChartEventPresentationTimeline timeline;
    private readonly GameTooltipText tooltip;
    private bool active = true;

    public GameEventPresentationController(Game game)
    {
        this.game = game;
        timeline = new ChartEventPresentationTimeline(game.Chart.Model, Debug.LogWarning);

        foreach (var candidate in Object.FindObjectsByType<GameTooltipText>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (candidate.game != game) continue;
            tooltip = candidate;
            break;
        }

        game.onGameCompleted.AddListener(_ => Release());
        game.onGameFailed.AddListener(_ => Release());
        game.onGameBeforeExit.AddListener(_ => Release());
        game.onGameDisposed.AddListener(_ => Release());
    }

    public void Apply(float time)
    {
        if (!active) return;
        var state = timeline.Evaluate(time);
        tooltip?.ApplyChartEventState(state);
        if (Scanner.Instance != null) Scanner.Instance.SetChartEventColor(state.ScanlineColor);
    }

    public void Release()
    {
        if (!active) return;
        active = false;
        tooltip?.ClearChartEventState();
        if (Scanner.Instance != null) Scanner.Instance.SetChartEventColor(Color.white);
    }
}
