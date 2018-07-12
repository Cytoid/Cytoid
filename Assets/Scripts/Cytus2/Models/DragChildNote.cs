using System.Collections;
using Cytus2.Views;
using UnityEngine;

namespace Cytus2.Models
{
    public class DragChildNote : GameNote
    {
        
        protected override void Awake()
        {
            base.Awake();
            View = new DragChildNoteView(this);
            MaxMissThreshold = 0.150f;
        }
        
        public override void Touch(Vector2 screenPos)
        {
            // Do not handle touch event if touched too ahead of scanner
            if (Note.start_time - Game.Time > 0.31f) return;
            // Do not handle touch event if in a later page, unless the timing is close (half a screen)
            if (Note.page_index > Game.CurrentPageId && Note.start_time - Game.Time > Page.Duration / 2f) return;
            base.Touch(screenPos);
        }
        
        public override NoteGrading CalculateGrading()
        {
            var ranking = NoteGrading.Miss;
            if (TimeUntilStart >= 0)
            {
                ranking = NoteGrading.Undetermined;
                if (TimeUntilStart < 0.250f)
                {
                    ranking = NoteGrading.Perfect;
                }
            }
            else
            {
                var timePassed = -TimeUntilStart;
                if (timePassed < 0.100f)
                {
                    ranking = NoteGrading.Perfect;
                }
            }
            return ranking;
        }
        
        protected override IEnumerator DestroyLater()
        {
            while (Game.Time < Note.start_time)
            {
                yield return null;
            }
            Destroy(gameObject);
        }

    }
}