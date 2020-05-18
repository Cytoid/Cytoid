using DragonBones;
using UnityEngine;

public class DefaultDragHeadNoteRenderer : DefaultNoteRenderer
{
    public DragHeadNote DragHeadNote => (DragHeadNote) Note;

    public DefaultDragHeadNoteRenderer(Note note) : base(note)
    {
    }

    protected override void UpdateComponentStates()
    {
        if (ArmatureComponent == null) return;
        if (Game.State.IsJudged(DragHeadNote.EndNoteModel.id))
        {
            Object.Destroy(ArmatureComponent.gameObject);
            ArmatureComponent = null;
        }
        else
        {
            base.UpdateComponentStates();
        }
    }

    protected override UnityDragonBonesData DragonBonesData() =>
        DefaultNoteRendererProvider.Instance.DragDragonBonesData;


    protected override string IntroAnimationName() => "出现";

    protected override float DragonBonesScaleMultiplier() => 1 / 5f;

}