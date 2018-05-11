using System;
using System.Collections.Generic;
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

    private bool playedHitSound;
    
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
            if (!playedHitSound && note.time <= game.TimeElapsed)
            {
                playedHitSound = true;
                hitSoundSource.Play();
            }
            // Already completed?
            if (game.TimeElapsed >= note.time + note.duration)
            {
                StopHolding();
            }
        }
    }
    
    public override void Touch(Vector2 touchScreenPosition)
    {
        // Do nothing
        rankedNoteData.press_time = TimeExt.Millis();
        rankedNoteData.press_x = (int) touchScreenPosition.x;
        rankedNoteData.press_y = (int) touchScreenPosition.y;
    }

    public List<int> fingers = new List<int>(2);

    public void StartHoldBy(int fingerIndex)
    {
        fingers.Add(fingerIndex);
        if (!isHolding)
        {
            StartHolding();
        }
    }

    public void StopHoldBy(int fingerIndex)
    {
        fingers.Remove(fingerIndex);
        if (isHolding && fingers.Count == 0)
        {
            StopHolding();
        }
    }

    public bool isHeldLate;
    public float lateTiming;

    public void StartHolding()
    {
        if (!game.IsLoaded || game.IsPaused) return;
        if (isHolding) return;
        isHolding = true;
        if (game.TimeElapsed > note.time)
        {
            isHeldLate = true;
            lateTiming = game.TimeElapsed - note.time;
        }
    }

    public void StopHolding()
    {
        if (!isHolding) return;
        isHolding = false;
        if (note.time < game.TimeElapsed)
        {
            if (hitSoundSource.clip != null)
            {
                hitSoundSource.Play();
            }
            Clear(CalculateGrading());
        }
    }

    public override void Clear(NoteGrading grading)
    {
        if (cleared)
        {
            Debug.LogError("This note is cleared already.");
        }
        base.Clear(grading);
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

    public override NoteGrading CalculateGrading()
    {
        var grading = NoteGrading.Miss;
        var rankGrading = NoteGrading.Miss;
        if (heldDuration > note.duration - 0.05)
        {
            grading = NoteGrading.Perfect;
        }
        else if (heldDuration > note.duration * 0.7f)
        {
            grading = NoteGrading.Great;
        }
        else if (heldDuration > note.duration * 0.5f)
        {
            grading = NoteGrading.Good;
        }
        else if (heldDuration > note.duration * 0.3f)
        {
            grading = NoteGrading.Bad;
        }
        if (game.IsRanked)
        {
            if (isHeldLate)
            {
                if (lateTiming < 0.200f)
                {
                    rankGrading = NoteGrading.Bad;
                }
                if (lateTiming < 0.150f)
                {
                    rankGrading = NoteGrading.Good;
                }
                if (lateTiming < 0.070f)
                {
                    rankGrading = NoteGrading.Great;
                }
                if (lateTiming <= 0.040f)
                {
                    rankGrading = NoteGrading.Perfect;
                }
                if (rankGrading == NoteGrading.Great) {
                    GreatGradeWeight = 1 - (lateTiming - 0.04) / (0.07 - 0.04);
                }
            }
            else
            {
                rankGrading = grading;
                if (rankGrading == NoteGrading.Great) {
                    GreatGradeWeight = 1 - (heldDuration - note.duration * 0.7f) / (note.duration - 0.05 - note.duration * 0.7f);
                }
            }
        }
        if (game.IsRanked && rankGrading > grading) return rankGrading;
        return grading;
    }

}