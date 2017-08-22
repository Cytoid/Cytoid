using UnityEngine;

public class ChainNoteView : NoteView
{
    
    public GameObject chainRendererPrefab;
    protected LineRenderer chainRenderer;
    
    public override float Size
    {
        get
        {
            if (note.isChainHead) return layout.NoteChainHeadSize;
            return layout.NoteChainSize;
        }
    }

    protected override void OnDisplay()
    {
        base.OnDisplay();
        // Connected note
        if (note.connectedNote != null)
        {
            var connectedNoteView = game.NoteViews[note.connectedNote.id];
            if (chainRenderer == null)
            {
                chainRenderer = Instantiate(chainRendererPrefab, transform.parent).GetComponent<LineRenderer>();
            }
            var chainZ = Mathf.Min(transform.position.z, connectedNoteView.transform.position.z) + 1;
            var posDiff = new Vector3(connectedNoteView.transform.position.x,
                              connectedNoteView.transform.position.y, 0)
                          - new Vector3(transform.position.x, transform.position.y, 0);
            var thisVec = posDiff.normalized * TimeScaledSize * 0.45f;
            var thatVec = posDiff.normalized * connectedNoteView.TimeScaledSize * 0.45f;
            var newColor = chainRenderer.startColor;
            newColor.a = ringSpriteRenderer.color.a;
            chainRenderer.startColor = newColor;
            chainRenderer.endColor = newColor;
            chainRenderer.SetPosition(0, new Vector3(transform.position.x, transform.position.y, chainZ) + thisVec);
            chainRenderer.SetPosition(1, new Vector3(connectedNoteView.transform.position.x, connectedNoteView.transform.position.y, chainZ) - thatVec);
            chainRenderer.widthMultiplier = 1.0f;
        }
    }

    public override void Touch()
    {
        if (!game.IsLoaded || game.IsPaused) return;
        // Do not handle touch event if touched too ahead of scanner
        if (note.time - game.TimeElapsed > 0.31f) return;
        // Do not handle touch event if in a later page
        if (page > game.CurrentPage) return;
        
        base.Touch();
    }
    
    protected virtual bool IsMissed()
    {
        return TimeUntil < -(Mathf.Max(note.isChainHead ? 0.300f : 0.150f, note.duration));
    }

    public override NoteRanking CalculateRank()
    {
        var ranking = NoteRanking.Miss;
        var timeUntil = TimeUntil;
        if (timeUntil >= 0)
        {
            if (timeUntil < (note.isChainHead ? 0.800f : 0.400f))
            {
                ranking = NoteRanking.Excellent;
            }
            if (timeUntil < 0.200f)
            {
                ranking = NoteRanking.Perfect;
            }
        }
        else
        {
            var timePassed = -timeUntil;
            if (timePassed < (note.isChainHead ? 0.300f : 0.150f))
            {
                ranking = NoteRanking.Excellent;
            }
            if (timePassed < 0.100f)
            {
                ranking = NoteRanking.Perfect;
            }
        }
        return ranking;
    }

    public override void Clear(NoteRanking ranking)
    {
        base.Clear(ranking);
        if (chainRenderer != null)
        {
            Destroy(chainRenderer.gameObject);
        }
    }
    
}