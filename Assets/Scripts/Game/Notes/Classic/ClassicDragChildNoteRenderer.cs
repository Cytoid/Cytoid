using UnityEngine;

public class ClassicDragChildNoteRenderer : ClassicNoteRenderer
{
    protected readonly SpriteMask SpriteMask;
        
    public ClassicDragChildNoteRenderer(DragChildNote dragChildNote) : base(dragChildNote)
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
        var size = BaseTransformSize * Note.Model.Override.SizeMultiplier;
        
        var minSize = Note.Model.initial_scale;
        var timeScale = Mathf.Clamp((Game.Time - Note.Model.intro_time) / (Note.Model.start_time - Note.Model.intro_time), 0f, 1f);
        var timeScaledSize = size * minSize + size * (1 - minSize) * timeScale;

        Note.transform.SetLocalScaleXY(timeScaledSize, timeScaledSize);
    }

    protected override void UpdateFillScale()
    {
    }

}