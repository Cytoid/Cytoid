using Cytoid.Storyboard.Sprites;
using UnityEngine;

namespace Cytoid.Storyboard.Notes
{
    public class NoteControllerEaser : StoryboardRendererEaser<NoteControllerState>
    {
        private NoteControllerRenderer NoteControllerRenderer { get; }

        private ChartModel.Note Note => NoteControllerRenderer.Note;

        public NoteControllerEaser(NoteControllerRenderer renderer) : base(renderer.MainRenderer)
        {
            NoteControllerRenderer = renderer;
        }

        public override void OnUpdate()
        {
            NotePositionSampler.Apply(Note.Override, From, To, Time);

            if (From.OverrideRotX != null)
            {
                if (From.OverrideRotX.Value)
                {
                    Note.Override.RotX = From.RotX != null ? EaseFloat(From.RotX, To.RotX) : 0;
                }
                else
                {
                    Note.Override.RotX = null;
                }
            }

            if (From.OverrideRotY != null)
            {
                if (From.OverrideRotY.Value)
                {
                    Note.Override.RotY = From.RotY != null ? EaseFloat(From.RotY, To.RotY) : 0;
                }
                else
                {
                    Note.Override.RotY = null;
                }
            }

            if (From.OverrideRotZ != null)
            {
                if (From.OverrideRotZ.Value)
                {
                    Note.Override.RotZ = From.RotZ != null ? EaseFloat(From.RotZ, To.RotZ) : 0;
                }
                else
                {
                    Note.Override.RotZ = null;
                }
            }

            if (From.OverrideRingColor != null)
            {
                if (From.OverrideRingColor.Value)
                {
                    Note.Override.RingColor = EaseColor(From.RingColor, To.RingColor);
                }
                else
                {
                    Note.Override.RingColor = null;
                }
            }

            if (From.OverrideFillColor != null)
            {
                if (From.OverrideFillColor.Value)
                {
                    Note.Override.FillColor = EaseColor(From.FillColor, To.FillColor);
                }
                else
                {
                    Note.Override.FillColor = null;
                }
            }

            if (From.OpacityMultiplier != null)
            {
                Note.Override.OpacityMultiplier = EaseFloat(From.OpacityMultiplier, To.OpacityMultiplier);
            }

            if (From.SizeMultiplier != null)
            {
                Note.Override.SizeMultiplier = EaseFloat(From.SizeMultiplier, To.SizeMultiplier);
            }

            if (From.HitboxMultiplier != null)
            {
                Note.Override.HitboxMultiplier = EaseFloat(From.HitboxMultiplier, To.HitboxMultiplier);
            }

            if (From.HoldDirection != null)
            {
                // Keep the legacy shared-field write (alt color / selectors read it),
                // and mirror into Override so the hold renderer sees an explicit,
                // absolute flip independent of page travel (#212).
                Note.direction = From.HoldDirection.Value;
                Note.Override.HoldDirection = From.HoldDirection;
            }

            if (From.Style != null)
            {
                Note.style = From.Style.Value;
            }
        }
    }
}