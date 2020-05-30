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
            if (From.OverrideX != null)
            {
                if (From.OverrideX.Value)
                {
                    Note.Override.X = From.X != null ? EaseFloat(From.X, To.X) : 0.5f;
                }
                else
                {
                    Note.Override.X = null;
                }
            }

            if (From.OverrideY != null)
            {
                if (From.OverrideY.Value)
                {
                    Note.Override.Y = From.Y != null ? EaseFloat(From.Y, To.Y) : 0.5f;
                }
                else
                {
                    Note.Override.Y = null;
                }
            }

            if (From.OverrideZ != null)
            {
                if (From.OverrideZ.Value)
                {
                    Note.Override.Z = From.Z != null ? EaseFloat(From.Z, To.Z) : 0;
                }
                else
                {
                    Note.Override.Z = null;
                }
            }

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

            if (From.XMultiplier != null)
            {
                Note.Override.XMultiplier = EaseFloat(From.XMultiplier, To.XMultiplier);
            }

            if (From.YMultiplier != null)
            {
                Note.Override.YMultiplier = EaseFloat(From.YMultiplier, To.YMultiplier);
            }

            if (From.XOffset != null)
            {
                Note.Override.XOffset = EaseFloat(From.XOffset, To.XOffset);
            }

            if (From.YOffset != null)
            {
                Note.Override.YOffset = EaseFloat(From.YOffset, To.YOffset);
            }

            if (From.HoldDirection != null)
            {
                Note.direction = From.HoldDirection.Value;
            }

            if (From.Style != null)
            {
                Note.style = From.Style.Value;
            }
        }
    }
}