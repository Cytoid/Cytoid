using System.Collections.Generic;
using Cytus2.Views;
using UnityEngine;

namespace Cytus2.Models
{
    public class HoldNote : GameNote
    {
        
        public bool IsHolding;
        public float HoldingStartTime;
        public float HeldDuration;
        public float Progress;

        private readonly List<int> holdingFingers = new List<int>(2);

        protected override void Awake()
        {
            base.Awake();
            View = new HoldNoteView(this);
        }

        public void StartHolding()
        {
            if (IsHolding) return;
            IsHolding = true;
            HoldingStartTime = Game.Time;
            ((HoldNoteView) View).OnStartHolding();
        }

        public void StopHolding()
        {
            if (!IsHolding) return;
            IsHolding = false;
            ((HoldNoteView) View).OnStopHolding();

            if (Note.start_time < Game.Time)
            {
                Clear(CalculateGrading());
            }
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (IsHolding)
            {
                HeldDuration = Game.Time - HoldingStartTime;
                Progress = (Game.Time - Note.start_time) / Note.Duration;

                // Already completed?
                if (Game.Time >= Note.end_time)
                {
                    StopHolding();
                }
            }
            else
            {
                Progress = 0;
            }
        }

        public override bool IsMissed()
        {
            return !IsHolding && base.IsMissed();
        }

        public override void Touch(Vector2 screenPos)
        {
            // Do nothing

            // TODO: Rank data
        }

        public void StartHoldingBy(int finger)
        {
            holdingFingers.Add(finger);
            StartHolding();
        }

        public void StopHoldingBy(int finger)
        {
            holdingFingers.Remove(finger);
            if (holdingFingers.Count == 0)
            {
                StopHolding();
            }
        }

        public override NoteGrading CalculateGrading()
        {
            var grading = NoteGrading.Miss;
            var rankGrading = NoteGrading.Miss;
            if (HeldDuration > Note.Duration - 0.05f)
            {
                grading = NoteGrading.Perfect;
            }
            else if (HeldDuration > Note.Duration * 0.7f)
            {
                grading = NoteGrading.Great;
            }
            else if (HeldDuration > Note.Duration * 0.5f)
            {
                grading = NoteGrading.Good;
            }
            else if (HeldDuration > Note.Duration * 0.3f)
            {
                grading = NoteGrading.Bad;
            }

            if (Game.Play.IsRanked)
            {
                if (HoldingStartTime > Note.start_time)
                {
                    var lateBy = HoldingStartTime - Note.start_time;
                    if (lateBy < 0.200f)
                    {
                        rankGrading = NoteGrading.Bad;
                    }

                    if (lateBy < 0.150f)
                    {
                        rankGrading = NoteGrading.Good;
                    }

                    if (lateBy < 0.070f)
                    {
                        rankGrading = NoteGrading.Great;
                    }

                    if (lateBy <= 0.040f)
                    {
                        rankGrading = NoteGrading.Perfect;
                    }

                    if (rankGrading == NoteGrading.Great)
                    {
                        GreatGradeWeight = 1.0f - (lateBy - 0.040f) / (0.070f - 0.040f);
                    }
                }
                else
                {
                    rankGrading = grading;
                    if (rankGrading == NoteGrading.Great)
                    {
                        GreatGradeWeight = 1.0f - (HeldDuration - Note.Duration * 0.70f) /
                                           (Note.Duration - 0.050f - Note.Duration * 0.70f);
                    }
                }
            }

            if (Game.Play.IsRanked && rankGrading > grading)
                return rankGrading; // Return the "worse" ranking (Note miss > bad > good > great > perfect)
            return grading;
        }
        
    }
    
}