public class GamePlayState
{
    public string ActivePlayId { get; private set; }

    public bool IsReadyForBridge { get; set; }

    public bool IsPlayRoutePaused { get; private set; }

    public bool HasActivePlay => !string.IsNullOrEmpty(ActivePlayId);

    public void SetActivePlay(string playId)
    {
        ActivePlayId = playId;
        IsPlayRoutePaused = false;
    }

    public void ClearActivePlay()
    {
        ActivePlayId = null;
    }

    public void MarkPlayRouteEnded()
    {
        IsPlayRoutePaused = true;
        ClearActivePlay();
    }
}
