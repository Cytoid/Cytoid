using UnityEngine;

namespace Cytoid.Storyboard.Notes
{
    public class NoteControllerEaser : StoryboardRendererEaser<NoteControllerState>
    {
        public NoteControllerEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (From.Note != null)
            {
                var note = Game.Chart.Model.note_map[From.Note.Value];

                if (From.OverrideX.IsSet())
                {
                    if (From.OverrideX.Value)
                    {
                        note.Override.X = From.X.IsSet() ? EaseFloat(From.X, To.X) : 0.5f;
                    }
                    else
                    {
                        note.Override.X = null;
                    }
                }

                if (From.OverrideY.IsSet())
                {
                    if (From.OverrideY.Value)
                    {
                        note.Override.Y = From.Y.IsSet() ? EaseFloat(From.Y, To.Y) : 0.5f;
                    }
                    else
                    {
                        note.Override.Y = null;
                    }
                }

                if (From.OverrideZ.IsSet())
                {
                    if (From.OverrideZ.Value)
                    {
                        note.Override.Z = From.Z.IsSet() ? EaseFloat(From.Z, To.Z) : 0;
                    }
                    else
                    {
                        note.Override.Z = null;
                    }
                }

                if (From.OverrideRotX.IsSet())
                {
                    if (From.OverrideRotX.Value)
                    {
                        note.Override.RotX = From.RotX.IsSet() ? EaseFloat(From.RotX, To.RotX) : 0;
                    }
                    else
                    {
                        note.Override.RotX = null;
                    }
                }

                if (From.OverrideRotY.IsSet())
                {
                    if (From.OverrideRotY.Value)
                    {
                        note.Override.RotY = From.RotY.IsSet() ? EaseFloat(From.RotY, To.RotY) : 0;
                    }
                    else
                    {
                        note.Override.RotY = null;
                    }
                }

                if (From.OverrideRotZ.IsSet())
                {
                    if (From.OverrideRotZ.Value)
                    {
                        note.Override.RotZ = From.RotZ.IsSet() ? EaseFloat(From.RotZ, To.RotZ) : 0;
                    }
                    else
                    {
                        note.Override.RotZ = null;
                    }
                }

                if (From.OverrideRingColor.IsSet())
                {
                    if (From.OverrideRingColor.Value)
                    {
                        note.Override.RingColor = EaseColor(From.RingColor, To.RingColor);
                    }
                    else
                    {
                        note.Override.RingColor = null;
                    }
                }

                if (From.OverrideFillColor.IsSet())
                {
                    if (From.OverrideFillColor.Value)
                    {
                        note.Override.FillColor = EaseColor(From.FillColor, To.FillColor);
                    }
                    else
                    {
                        note.Override.FillColor = null;
                    }
                }

                if (From.OpacityMultiplier.IsSet())
                {
                    note.Override.OpacityMultiplier = EaseFloat(From.OpacityMultiplier, To.OpacityMultiplier);
                }

                if (From.SizeMultiplier.IsSet())
                {
                    note.Override.SizeMultiplier = EaseFloat(From.SizeMultiplier, To.SizeMultiplier);
                }

                if (From.XMultiplier.IsSet())
                {
                    note.Override.XMultiplier = EaseFloat(From.XMultiplier, To.XMultiplier);
                }

                if (From.YMultiplier.IsSet())
                {
                    note.Override.YMultiplier = EaseFloat(From.YMultiplier, To.YMultiplier);
                }

                if (From.XOffset.IsSet())
                {
                    note.Override.XOffset = EaseFloat(From.XOffset, To.XOffset);
                }

                if (From.YOffset.IsSet())
                {
                    note.Override.YOffset = EaseFloat(From.YOffset, To.YOffset);
                }

                if (From.HoldDirection.IsSet())
                {
                    note.direction = From.HoldDirection;
                }

                if (From.Style.IsSet())
                {
                    note.style = From.Style;
                }
            }
        }
    }
}