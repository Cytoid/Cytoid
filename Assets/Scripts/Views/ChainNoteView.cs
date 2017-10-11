using System.Collections;
using QuickEngine.Extensions;
using UnityEngine;

public class ChainNoteView : NoteView
{
    public GameObject chainRendererPrefab;
    public GameObject chainHeadPrefab;
    protected LineRenderer chainRenderer;

    [HideInInspector] public ChainHeadView chainHead;
    [HideInInspector] public ChainNoteView connectedNoteView;

    private Vector3 originalPosition;
    private Vector3 thisVec;
    private Vector3 thatVec;
    private Vector3 direction;
    private bool initializedChainHead;
    private bool drawChain = true;

    public override float Size
    {
        get { return layout.NoteChainSize; }
    }

    public override void OnAllNotesInitialized()
    {
        base.OnAllNotesInitialized();
        if (note.connectedNote != null)
        {
            connectedNoteView = game.NoteViews[(object) note.connectedNote.id] as ChainNoteView;
            direction = new Vector3(connectedNoteView.transform.position.x, connectedNoteView.transform.position.y, 0)
                - new Vector3(transform.position.x, transform.position.y, 0);

            direction.Normalize();

            thatVec = direction * 0.4f;
        }
        originalPosition = transform.position;
    }

    protected override void OnDisplay()
    {
        base.OnDisplay();
        ringSpriteRenderer.enabled = false;
        if (!initializedChainHead && note.isChainHead && chainHead == null)
        {
            initializedChainHead = true;
            chainHead = Instantiate(chainHeadPrefab, transform.position.SetZ(transform.position.z - 0.05f),
                Quaternion.identity, transform.parent).GetComponent<ChainHeadView>();
            chainHead.game = game;
            chainHead.nextNoteView = this;
            ringSpriteRenderer.enabled = false;
            fillSpriteRenderer.enabled = false;
        }
        if (chainRenderer != null)
        {
            chainRenderer.enabled = drawChain;
        }
    }

    private void Update()
    {
        if (connectedNoteView != null && (connectedNoteView.cleared || connectedNoteView.TimeUntil < 0) && chainRenderer != null)
        {
            chainRenderer.enabled = false;
            return;
        }
        // Connected note
        if (displayed && connectedNoteView != null && chainHead != null)
        {
            if (connectedNoteView.chainHead == null)
            {
                connectedNoteView.chainHead = chainHead;
            }
            if (chainRenderer == null)
            {
                chainRenderer = Instantiate(chainRendererPrefab, transform.parent).GetComponent<LineRenderer>();
            }
            if (connectedNoteView.TimeUntil < 0)
            {
                chainRenderer.enabled = false;
                return;
            }
            
            thisVec = direction;
            if (TimeUntil < -0.05)
            {
                thisVec = thisVec * 0.56f / 1.2f * 1.5f;
            }
            else
            {
                if (note.isChainHead)
                {
                    thisVec = thisVec * 0.56f / 1.2f * 1.5f;
                }
                else
                {
                    thisVec = thisVec * 0.4f;
                }
            }
            
            var chainZ = Mathf.Min(originalPosition.z, connectedNoteView.originalPosition.z) + 1;          
            var newColor = chainRenderer.startColor;
            newColor.a = ringSpriteRenderer.color.a;
            chainRenderer.startColor = newColor;
            chainRenderer.endColor = newColor;

            Vector3 startPos;
            if (TimeUntil < -0.05)
            {
                startPos = new Vector3(chainHead.transform.position.x, chainHead.transform.position.y, chainZ) + thisVec;
            }
            else
            {
                startPos = new Vector3(originalPosition.x, originalPosition.y, chainZ) + thisVec;
            }
            var endPos =
                new Vector3(connectedNoteView.originalPosition.x, connectedNoteView.originalPosition.y, chainZ) -
                thatVec;

            if ((connectedNoteView.transform.position.y > transform.position.y && endPos.y < startPos.y) 
                || 
                (connectedNoteView.transform.position.y < transform.position.y && endPos.y > startPos.y))
            {
                drawChain = false;
                return;
            }
            
            chainRenderer.SetPosition(0, startPos);
            chainRenderer.SetPosition(1, endPos);
            chainRenderer.widthMultiplier = 1.0f;
        }
    }

    public override void Touch()
    {
        if (!game.IsLoaded || game.IsPaused) return;
        // Do not handle touch event if touched too ahead of scanner
        if (note.time - game.TimeElapsed > 0.31f) return;
        // Do not handle touch event if in a later page, unless the timing is close (half a screen)
        if (page > game.CurrentPage && note.time - game.TimeElapsed > Chart.pageDuration / 2f) return;

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
        if (connectedNoteView == null && chainHead != null) // Last chain note
        {
            Destroy(chainHead.gameObject);
        }
    }

    public override IEnumerator DestroyLater()
    {
        yield return new WaitForSeconds(3);
        chainHead = null;
        game.NoteViews.Remove(note.id);
        Destroy(gameObject);
        if (chainRenderer != null)
        {
            Destroy(chainRenderer.gameObject);
        }
    }

    protected override IEnumerator EmergeAnim()
    {
        ringSpriteRenderer.color = new Color(ringSpriteRenderer.color.r, ringSpriteRenderer.color.g,
            ringSpriteRenderer.color.b, ringSpriteRenderer.color.a + 0.05f);
        fillSpriteRenderer.color = new Color(fillSpriteRenderer.color.r, fillSpriteRenderer.color.g,
            fillSpriteRenderer.color.b, fillSpriteRenderer.color.a + 0.05f);
        yield return new WaitForSeconds(0.01f);
        if (ringSpriteRenderer.color.a < 0.5f)
        {
            yield return StartCoroutine(EmergeAnim());
        }
        else
        {
            yield return null;
        }
    }

    protected override IEnumerator OpaqueAnim()
    {
        ringSpriteRenderer.color = new Color(ringSpriteRenderer.color.r, ringSpriteRenderer.color.g,
            ringSpriteRenderer.color.b, ringSpriteRenderer.color.a + 0.05f);
        fillSpriteRenderer.color = new Color(fillSpriteRenderer.color.r, fillSpriteRenderer.color.g,
            fillSpriteRenderer.color.b, fillSpriteRenderer.color.a + 0.05f);
        yield return new WaitForSeconds(0.01f);
        if (ringSpriteRenderer.color.a < 1f)
        {
            yield return StartCoroutine(OpaqueAnim());
        }
        else
        {
            yield return null;
        }
    }
}