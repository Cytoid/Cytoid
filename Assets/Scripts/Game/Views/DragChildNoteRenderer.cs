using UnityEngine;

public class DragChildNoteRenderer : ClassicNoteRenderer
{
    protected SpriteMask SpriteMask;
        
    public DragChildNoteRenderer(DragChildNote dragChildNote) : base(dragChildNote)
    {
        SpriteMask = Note.transform.GetComponentInChildren<SpriteMask>();
    }

    public override void OnNoteLoaded()
    {
        base.OnNoteLoaded();
        SpriteMask.frontSortingOrder = Note.Model.id + 1;
        SpriteMask.backSortingOrder = Note.Model.id - 2;
    }

    protected override void UpdateComponentStates()
    {
        base.UpdateComponentStates();
        Ring.enabled = false; // Always disable ring
        if (Note.IsCleared)
        {
            Fill.enabled = Game.Time <= Note.Model.start_time; // Enable fill until drag head passes by
        }
        else
        {
            SpriteMask.enabled = Game.Time >= Note.Model.intro_time;
        }
    }

    protected override void UpdateTransformScale()
    {
        const float minSize = 0.7f;
        var timeRequired = 1.175f / Note.Model.speed;
        var timeScale = Mathf.Clamp((Game.Time - Note.Model.intro_time) / timeRequired, 0f, 1f);
        var timeScaledSize = BaseSize * minSize + BaseSize * (1 - minSize) * timeScale;

        Note.transform.SetLocalScaleXY(timeScaledSize, timeScaledSize);
    }

    protected override void UpdateFillScale()
    {
    }

}