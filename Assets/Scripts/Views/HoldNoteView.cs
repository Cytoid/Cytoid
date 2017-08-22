using System;
using System.Linq;
using UnityEngine;

public class HoldNoteView : NoteView
{
    
    public GameObject holdRailPrefab;
    public GameObject holdCartPrefab;
    
    protected SpriteRenderer holdRail;
    protected SpriteRenderer holdCart;
    protected SpriteRenderer mask;

    public bool isHolding;
    public float heldDuration;

    private int waitBetweenHoldFX;
    private int maxWaitBetweenHoldFX = 9;

    protected override void Awake()
    {
        base.Awake();
        waitBetweenHoldFX = maxWaitBetweenHoldFX;
        mask = transform.Find("Mask").GetComponent<SpriteRenderer>();
        mask.enabled = false;
    }

    protected override bool IsMissed()
    {
        return !isHolding && base.IsMissed();
    }

    protected override void OnDisplay()
    {
        base.OnDisplay();
        // Hold note
        if (note.duration > 0)
        {
            if (holdRail == null)
            {
                holdRail = Instantiate(holdRailPrefab, transform.parent).GetComponent<SpriteRenderer>();
            }
            if (holdCart == null && note.time < game.TimeElapsed)
            {
                holdCart = Instantiate(holdCartPrefab, transform.parent).GetComponent<SpriteRenderer>();
            }

            holdRail.color = ringColor;
            var newColor = holdRail.color;
            newColor.a = ringSpriteRenderer.color.a;
            holdRail.color = newColor;

            float t;

            var startY = transform.position.y + (endY > y ? 1 : -1) * (layout.NoteSize * 0.5f * 0.75f);
            
            if (note.time <= game.TimeElapsed) t = 1;
            else t = 1 - Mathf.Clamp((TimeDiff - 0.25f) / Chart.pageDuration, 0f, 1f);
            var lerpEndY = Mathf.Lerp(startY, endY, t);

            ScaleToTwoEnds(holdRail.transform, startY, lerpEndY);

            if (holdCart != null)
            {
                holdCart.color = fillColor;
                
                lerpEndY = ScannerView.Instance.transform.position.y;
                ScaleToTwoEnds(holdCart.transform, startY, lerpEndY);

                holdCart.enabled = isHolding;
            }
        }
    }

    private void ScaleToTwoEnds(Transform transform, float startY, float endY)
    {
        var centerPos = new Vector3(this.transform.position.x, (startY + endY) / 2f,
            this.transform.position.z + 0.05f);
        transform.position = centerPos;
        transform.localScale = new Vector3(1, Mathf.Abs(endY - startY), 1);
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (!cleared)
        {
            mask.enabled = isHolding;
            if (isHolding && note.time < game.TimeElapsed)
            {
                heldDuration += Time.fixedDeltaTime;
            }
        }
        if (isHolding)
        {
            // Play hold FX
            if (note.time <= game.TimeElapsed + 0.2f)
            {
                if (waitBetweenHoldFX == maxWaitBetweenHoldFX)
                {
                    waitBetweenHoldFX = 0;
                    particleManager.PlayHoldFX(this);
                }
                waitBetweenHoldFX++;
            }
            // Already completed?
            if (game.TimeElapsed >= note.time + note.duration)
            {
                StopHolding();
            }
        }
    }

    public override void Touch()
    {
        // Do nothing
    }

    public void StartHolding()
    {
        if (!game.IsLoaded || game.IsPaused) return;
        if (isHolding) return;
        isHolding = true;
    }

    public void StopHolding()
    {
        if (!isHolding) return;
        isHolding = false;
        if (note.time < game.TimeElapsed)
        {
            Clear(CalculateRank());
        }
    }

    public override void Clear(NoteRanking ranking)
    {
        if (cleared)
        {
            Debug.LogError("This note is cleared already.");
        }
        base.Clear(ranking);
        if (holdRail != null)
        {
            Destroy(holdRail.gameObject);
        }
        if (holdCart != null)
        {
            Destroy(holdCart.gameObject);
        }
        if (mask != null)
        {
            Destroy(mask.gameObject);
        }
    }

    public override NoteRanking CalculateRank()
    {
        var ranking = NoteRanking.Miss;
        if (heldDuration > note.duration - 0.05)
        {
            ranking = NoteRanking.Perfect;
        }
        else if (heldDuration > note.duration * 0.7f)
        {
            ranking = NoteRanking.Excellent;
        }
        else if (heldDuration > note.duration * 0.5f)
        {
            ranking = NoteRanking.Good;
        }
        else if (heldDuration > note.duration * 0.3f)
        {
            ranking = NoteRanking.Bad;
        }
        return ranking;
    }
}