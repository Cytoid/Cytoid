using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class DragChildNote : Note
{

    protected override NoteRenderer CreateRenderer()
    {
        if (Game.Config.UseFlatNoteDesign) return new FlatDragChildNoteRenderer(this);
        return Game.Config.UseClassicStyle
            ? (NoteRenderer) new ClassicDragChildNoteRenderer(this)
            : throw new NotSupportedException();
    }

    public override bool CanHandleTouch() =>
        base.CanHandleTouch() && (UsesCy1Drag || CanHandleNativeDragTouch());

    internal override bool CanRepresentNativeDrag() =>
        CanHandleNativeDragTouch() && CalculateNativeDragGrade(0.250f, 0.100f) == NoteGrade.Perfect;

    public override NoteGrade CalculateGrade() => UsesCy1Drag
        ? base.CalculateGrade()
        : CalculateNativeDragGrade(0.250f, 0.100f);

    public override async void Collect()
    {
        if (IsCollected) return;
        
        bool CanCollect() => Game.Time >= Model.start_time;
        if (CanCollect())
        {
            base.Collect();
            return;
        }
        await UniTask.WaitUntil(() => IsCollected || CanCollect());
        base.Collect();
    }
        
    public override bool IsAutoEnabled()
    {
        return base.IsAutoEnabled() || Game.State.Mods.Contains(Mod.AutoDrag);
    }
    
    public override void PlayHitSound()
    {
        if (Context.AudioManager.IsSfxLoaded("HitSound"))
        {
            Context.AudioManager.GetSfx("HitSound").Play();
        }
        Context.Haptic(HapticTypes.Selection, false);
    }
}
