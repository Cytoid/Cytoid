using UnityEngine;

public class FlickNoteClassicRenderer : ClassicNoteRenderer
{
    private SpriteRenderer leftArrow;
    private SpriteRenderer rightArrow;

    private static float MaxArrowOffset = Camera.main.orthographicSize * 0.3f;

    public FlickNoteClassicRenderer(FlickNote flickNote) : base(flickNote)
    {
        leftArrow = Note.transform.Find("LeftArrow").GetComponent<SpriteRenderer>();
        rightArrow = Note.transform.Find("RightArrow").GetComponent<SpriteRenderer>();
        leftArrow.transform.SetLocalX(-MaxArrowOffset);
        rightArrow.transform.SetLocalX(MaxArrowOffset);
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
        leftArrow.transform.localPosition = Vector3.Lerp(
            new Vector3(-MaxArrowOffset, 0, 0),
            new Vector3(0, 0, 0),
            Mathf.Clamp((Game.Time - Note.Model.intro_time) / (Note.Model.start_time - Note.Model.intro_time - 0.25f),
                0, 1)
        );
        rightArrow.transform.localPosition = Vector3.Lerp(
            new Vector3(MaxArrowOffset, 0, 0),
            new Vector3(0, 0, 0),
            Mathf.Clamp((Game.Time - Note.Model.intro_time) / (Note.Model.start_time - Note.Model.intro_time - 0.25f),
                0, 1)
        );
    }
}