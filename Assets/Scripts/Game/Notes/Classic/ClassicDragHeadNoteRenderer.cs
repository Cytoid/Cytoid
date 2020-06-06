using UnityEngine;
using Object = UnityEngine.Object;

public class ClassicDragHeadNoteRenderer : ClassicNoteRenderer
{
    public DragHeadNote DragHeadNote => (DragHeadNote) Note;
    
    protected SpriteMask SpriteMask;
    protected SpriteRenderer CDragFill;
    
    public ClassicDragHeadNoteRenderer(DragHeadNote dragHeadNote) : base(dragHeadNote)
    {
        SpriteMask = Note.transform.GetComponentInChildren<SpriteMask>();
        CDragFill = Note.transform.Find("NoteRing/CDragFill")?.GetComponent<SpriteRenderer>();
    }

    public override void OnNoteLoaded()
    {
        base.OnNoteLoaded();
        Fill.sortingOrder = Ring.sortingOrder + 1;
        if (CDragFill != null) CDragFill.sortingOrder = Fill.sortingOrder + 1;
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
            if (Game.State.Mods.Contains(Mod.HideNotes))
            {
                Ring.enabled = false;
                Fill.enabled = false;
                if (CDragFill != null) CDragFill.enabled = false;
            }
            else
            {
                Ring.enabled = true;
                Fill.enabled = true;
                if (CDragFill != null) CDragFill.enabled = true;
                if (DisplayNoteId)
                {
                    NoteId.gameObject.SetActive(true);
                    NoteId.transform.localEulerAngles = new Vector3(0, 0, -Note.transform.localEulerAngles.z);
                }
            }
        }
        else
        {
            Ring.enabled = false;
            Fill.enabled = false;
            if (CDragFill != null) CDragFill.enabled = false;
            if (DisplayNoteId) NoteId.gameObject.SetActive(false);
        }
    }

    protected override void UpdateTransformScale()
    {
        if (DragHeadNote.IsCDrag)
        {
            base.UpdateTransformScale();
        }
        else
        {
            var minSize = Note.Model.initial_scale;
            var timeScale = Mathf.Clamp((Game.Time - Note.Model.intro_time) / (Note.Model.start_time - Note.Model.intro_time), 0f, 1f);
            var timeScaledSize = BaseTransformSize * minSize + BaseTransformSize * (1 - minSize) * timeScale;

            Note.transform.SetLocalScaleXY(timeScaledSize, timeScaledSize);
        }
    }

    protected override void UpdateFillScale()
    {
        if (DragHeadNote.IsCDrag)
        {
            base.UpdateFillScale();
        }
    }

    protected override void UpdateComponentOpacity()
    {
        base.UpdateComponentOpacity();
        if (DragHeadNote.IsCDrag)
        {
            CDragFill.color = Ring.color;
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        Object.Destroy(SpriteMask);
        if (CDragFill != null) Object.Destroy(CDragFill);
    }
}