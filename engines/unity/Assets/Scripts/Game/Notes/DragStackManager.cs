using System.Collections.Generic;

/// <summary>
/// Owns live <see cref="DragStackHost"/> instances for the current chart play.
/// Follower transform sync / judgment is driven by the primary note after its LateUpdate.
/// </summary>
public class DragStackManager
{
    private readonly Dictionary<int, DragStackHost> hostsByStackId = new Dictionary<int, DragStackHost>();
    private Game game;

    public void Bind(Game gameInstance)
    {
        game = gameInstance;
        hostsByStackId.Clear();
    }

    public void Dispose()
    {
        hostsByStackId.Clear();
        game = null;
    }

    public void Register(Note note)
    {
        if (game?.Chart == null || note?.Model == null) return;
        if (!game.Chart.NoteIdToDragStackId.TryGetValue(note.Model.id, out var stackId)) return;

        if (!hostsByStackId.TryGetValue(stackId, out var host))
        {
            host = new DragStackHost(stackId);
            hostsByStackId[stackId] = host;
        }

        host.Add(note);
    }

    public void OnNoteCollected(Note note)
    {
        var host = note?.DragStack;
        if (host == null) return;
        host.OnMemberCollected(note);
        if (host.Members.Count == 0)
        {
            hostsByStackId.Remove(host.StackId);
        }
    }

    /// <summary>
    /// Dissolves every live host, restoring followers to independent notes. The
    /// registry is emptied so a subsequent replan can register against fresh tables.
    /// </summary>
    public void DissolveAll()
    {
        foreach (var host in hostsByStackId.Values)
        {
            host.Dissolve();
        }

        hostsByStackId.Clear();
    }
}
