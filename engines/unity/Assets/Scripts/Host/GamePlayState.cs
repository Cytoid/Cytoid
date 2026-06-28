public class GamePlayState
{
    public string ActiveSessionId { get; private set; }

    public bool IsReadyForBridge { get; set; }

    public bool IsPlayRoutePaused { get; private set; }

    public bool HasActiveSession => !string.IsNullOrEmpty(ActiveSessionId);

    public void SetActiveSession(string sessionId)
    {
        ActiveSessionId = sessionId;
        IsPlayRoutePaused = false;
    }

    public void ClearActiveSession()
    {
        ActiveSessionId = null;
    }

    public void MarkPlayRouteEnded()
    {
        IsPlayRoutePaused = true;
        ClearActiveSession();
    }
}
