using UnityEngine;

public class ClassicFlickNoteRenderer : ClassicNoteRenderer
{
    private readonly SpriteRenderer leftArrow;
    private readonly SpriteRenderer rightArrow;

    private readonly float maxArrowOffset;

    public ClassicFlickNoteRenderer(FlickNote flickNote) : base(flickNote)
    {
        maxArrowOffset = Game.camera.orthographicSize * 0.3f;
        leftArrow = Note.transform.Find("LeftArrow").GetComponent<SpriteRenderer>();
        rightArrow = Note.transform.Find("RightArrow").GetComponent<SpriteRenderer>();
        leftArrow.transform.SetLocalX(-maxArrowOffset);
        rightArrow.transform.SetLocalX(maxArrowOffset);
        leftArrow.color = leftArrow.color.WithAlpha(0);
        rightArrow.color = rightArrow.color.WithAlpha(0);
    }

    protected override void Render()
    {
        base.Render();
        UpdateArrows();
    }

    protected override void UpdateComponentStates()
    {
        base.UpdateComponentStates();
        if (!Note.IsCleared && Game.Time >= Note.Model.intro_time && Game.Time <= Note.Model.end_time + Note.MissThreshold)
        {
            leftArrow.enabled = true;
            rightArrow.enabled = true;
            if (Game.State.Mods.Contains(Mod.HideNotes))
            {
                leftArrow.enabled = false;
                rightArrow.enabled = false;
            }
        }
        else
        {
            leftArrow.enabled = false;
            rightArrow.enabled = false;
        }
    }

    protected override void UpdateComponentOpacity()
    {
        base.UpdateComponentOpacity();
        leftArrow.color = leftArrow.color.WithAlpha(EasedOpacity);
        rightArrow.color = rightArrow.color.WithAlpha(EasedOpacity);
    }

    protected virtual void UpdateArrows()
    {
        // Prefer closing 0.25s before hit when approach is long enough. Cap early-close
        // at half the approach so animDuration stays continuous near the 0.25 boundary
        // (avoids the cliff: approach 0.25 → full window, 0.2501 → near-zero window).
        const float PreferredEarlyClose = 0.25f;
        var approachDuration = Note.Model.start_time - Note.Model.intro_time;
        var earlyClose = Mathf.Min(PreferredEarlyClose, Mathf.Max(0f, approachDuration) * 0.5f);
        var animDuration = Mathf.Max(0f, approachDuration - earlyClose);
        var progress = animDuration > 0f
            ? Mathf.Clamp01((Game.Time - Note.Model.intro_time) / animDuration)
            : 1f;

        leftArrow.transform.localPosition = Vector3.Lerp(
            new Vector3(-maxArrowOffset, 0, 0),
            new Vector3(0, 0, 0),
            progress
        );
        rightArrow.transform.localPosition = Vector3.Lerp(
            new Vector3(maxArrowOffset, 0, 0),
            new Vector3(0, 0, 0),
            progress
        );
    }
}
