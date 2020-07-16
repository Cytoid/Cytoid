using MoreMountains.NiceVibrations;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class DragChildNote : Note
{

    protected override NoteRenderer CreateRenderer()
    {
        return Game.Config.UseClassicStyle
            ? (NoteRenderer) new ClassicDragChildNoteRenderer(this)
            : new DefaultDragChildNoteRenderer(this);
    }

    public override void OnTouch(Vector2 screenPos)
    {
        // Do not handle touch event if touched too ahead of scanner
        if (Model.start_time - Game.Time > 0.31f) return;
        // Do not handle touch event if in a later page, unless the timing is close (half a screen)
        if (Model.page_index > Game.Chart.CurrentPageId && Model.start_time - Game.Time > Page.Duration / 2f) return;
        base.OnTouch(screenPos);
    }

    public override NoteGrade CalculateGrade()
    {
        var grade = NoteGrade.Miss;
        var timeUntil = TimeUntilStart + JudgmentOffset;
        if (timeUntil >= 0)
        {
            grade = NoteGrade.None;
            if (timeUntil < 0.250f)
            {
                grade = NoteGrade.Perfect;
            }
        }
        else
        {
            var timePassed = -timeUntil;
            if (timePassed < 0.100f)
            {
                grade = NoteGrade.Perfect;
            }
        }

        return grade;
    }

    public override async void Collect()
    {
        bool CanCollect() => Game.Time >= Model.start_time;
        if (CanCollect())
        {
            base.Collect();
            return;
        }
        await UniTask.WaitUntil(CanCollect);
        base.Collect();
    }
        
    public override bool IsAutoEnabled()
    {
        return base.IsAutoEnabled() || Game.State.Mods.Contains(Mod.AutoDrag);
    }
    
    public override void PlayHitSound()
    {
        if (Context.AudioManager.IsLoaded("HitSound"))
        {
            Context.AudioManager.Get("HitSound").Play();
        }
        Context.Haptic(HapticTypes.Selection, false);
    }
}