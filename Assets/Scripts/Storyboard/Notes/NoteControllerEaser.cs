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
                if (From.OverrideX.IsSet())
                {
                    if (From.OverrideX.Value)
                    {
                        Game.Config.NoteXOverride[From.Note.Value] = From.X.IsSet() ? EaseFloat(From.X, To.X) : 0.5f;
                    }
                    else
                    {
                        if (Game.Config.NoteXOverride.Remove(From.Note.Value))
                        {
                            var note = Game.Chart.Model.note_map[From.Note.Value];
                            note.position.x = Game.Chart.ConvertChartXToScreenX((float) note.x);
                        }
                    }
                }
                
                if (From.OverrideY.IsSet())
                {
                    if (From.OverrideY.Value)
                    {
                        Game.Config.NoteYOverride[From.Note.Value] = From.Y.IsSet() ? EaseFloat(From.Y, To.Y) : 0.5f;
                    }
                    else
                    {
                        if (Game.Config.NoteYOverride.Remove(From.Note.Value))
                        {
                            var note = Game.Chart.Model.note_map[From.Note.Value];
                            note.position.y = Game.Chart.GetNoteScreenY(note);
                        }
                    }
                }
                
                if (From.Rot.IsSet())
                {
                    var note = Game.Chart.Model.note_map[From.Note.Value];
                    note.rotation = From.Rot.IsSet() ? EaseFloat(From.Rot, To.Rot) : 0;
                }
                
                if (From.OverrideRingColor.IsSet())
                {
                    if (From.OverrideRingColor.Value)
                    {
                        Game.Config.NoteRingColorOverride[From.Note.Value] = EaseColor(From.RingColor, To.RingColor);
                    }
                    else
                    {
                        Game.Config.NoteRingColorOverride.Remove(From.Note.Value);
                    }
                }
                
                if (From.OverrideFillColor.IsSet())
                {
                    if (From.OverrideFillColor.Value)
                    {
                        Game.Config.NoteFillColorOverride[From.Note.Value] = EaseColor(From.FillColor, To.FillColor);
                    }
                    else
                    {
                        Game.Config.NoteFillColorOverride.Remove(From.Note.Value);
                    }
                }

                if (From.OpacityMultiplier.IsSet())
                {
                    Game.Config.NoteOpacityMultiplier[From.Note.Value] = EaseFloat(From.OpacityMultiplier, To.OpacityMultiplier);
                }
                
                if (From.SizeMultiplier.IsSet())
                {
                    Game.Config.NoteSizeMultiplier[From.Note.Value] = EaseFloat(From.SizeMultiplier, To.SizeMultiplier);
                }
                
                if (From.HoldDirection.IsSet())
                {
                    var note = Game.Chart.Model.note_map[From.Note.Value];
                    note.direction = From.HoldDirection;
                }
                
                if (From.Style.IsSet())
                {
                    var note = Game.Chart.Model.note_map[From.Note.Value];
                    note.style = From.Style;
                }
            }
        }
        
    }
}