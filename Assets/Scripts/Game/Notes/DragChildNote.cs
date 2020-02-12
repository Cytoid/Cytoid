using UniRx.Async;
using UnityEngine;

public class DragChildNote : Note
{

    protected override NoteRenderer CreateRenderer()
    {
        return new DragChildNoteClassicRenderer(this);
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
        if (TimeUntilStart >= 0)
        {
            grade = NoteGrade.None;
            if (TimeUntilStart < 0.250f)
            {
                grade = NoteGrade.Perfect;
            }
        }
        else
        {
            var timePassed = -TimeUntilStart;
            if (timePassed < 0.100f)
            {
                grade = NoteGrade.Perfect;
            }
        }

        return grade;
    }

    protected override async void AwaitAndDestroy()
    {
        await UniTask.WaitUntil(() => Game.Time >= Model.start_time);
        Destroy();
    }
        
    public override bool IsAutoEnabled()
    {
        return base.IsAutoEnabled() || Game.State.Mods.Contains(Mod.AutoDrag);
    }
}