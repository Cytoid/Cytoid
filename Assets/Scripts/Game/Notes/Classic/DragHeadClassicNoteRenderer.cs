using UnityEngine;
using Object = UnityEngine.Object;

public class DragHeadClassicNoteRenderer : ClassicNoteRenderer
{
    public DragHeadNote DragHeadNote => (DragHeadNote) Note;
    
    protected SpriteMask SpriteMask;

    public DragHeadClassicNoteRenderer(DragHeadNote dragHeadNote) : base(dragHeadNote)
    {
        SpriteMask = Note.transform.GetComponentInChildren<SpriteMask>();
    }

    public override void OnNoteLoaded()
    {
        base.OnNoteLoaded();
        Fill.sortingOrder = Ring.sortingOrder + 1;
        SpriteMask.frontSortingOrder = Note.Model.id + 1;
        SpriteMask.backSortingOrder = Note.Model.id - 2;
    }

    protected override void UpdateComponentStates()
    {
        if (!Note.IsCleared)
        {
            SpriteMask.enabled = Game.Time >= Note.Model.intro_time;
        }
        
        if (Game.Time >= Note.Model.intro_time && (!Game.State.IsJudged(DragHeadNote.EndNoteModel.id) || Game.Time < DragHeadNote.EndNoteModel.start_time))
        {
            Ring.enabled = true;
            Fill.enabled = true;
            if (Game.State.Mods.Contains(Mod.HideNotes))
            {
                Ring.enabled = false;
                Fill.enabled = false;
            }
        }
        else
        {
            Ring.enabled = false;
            Fill.enabled = false;
        }
    }

    protected override void UpdateTransformScale()
    {
        const float minSize = 0.7f;
        var timeRequired = 1.175f / Note.Model.speed;
        var timeScale = Mathf.Clamp((Game.Time - Note.Model.intro_time) / timeRequired, 0f, 1f);
        var timeScaledSize = BaseTransformSize * minSize + BaseTransformSize * (1 - minSize) * timeScale;

        Note.transform.SetLocalScaleXY(timeScaledSize, timeScaledSize);
    }

    protected override void UpdateFillScale()
    {
    }

    public override void Cleanup()
    {
        base.Cleanup();
        Object.Destroy(SpriteMask);
    }
}