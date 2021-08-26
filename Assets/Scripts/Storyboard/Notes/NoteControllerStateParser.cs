using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Cytoid.Storyboard.Notes
{
    public class NoteControllerStateParser : GenericStateParser<NoteControllerState>
    {
        public NoteControllerStateParser(Storyboard storyboard) : base(storyboard)
        {
        }

        public override void Parse(NoteControllerState state, JObject json, NoteControllerState baseState)
        {
            ParseObjectState(state, json, baseState);
            
            state.Note = (int?) json.SelectToken("note") ?? state.Note;
            
            state.OverrideX = (bool?) json.SelectToken("override_x") ?? state.OverrideX;
            state.X = ParseUnitFloat(json.SelectToken("x"), ReferenceUnit.NoteX, false, false) ?? state.X;
            state.OverrideY = (bool?) json.SelectToken("override_y") ?? state.OverrideY;
            state.Y = ParseUnitFloat(json.SelectToken("y"), ReferenceUnit.NoteY, false, false) ?? state.Y;
            state.OverrideZ = (bool?) json.SelectToken("override_z") ?? state.OverrideZ;
            state.Z = ParseUnitFloat(json.SelectToken("z"), ReferenceUnit.World, false, false) ?? state.Z;
            
            state.XMultiplier = (float?) json.SelectToken("x_multiplier") ?? state.XMultiplier;
            state.YMultiplier = (float?) json.SelectToken("y_multiplier") ?? state.YMultiplier;
            state.XOffset = (float?) json.SelectToken("dx") ?? state.XOffset;
            state.YOffset = (float?) json.SelectToken("dy") ?? state.YOffset; // TODO: This is broken
            // TODO: Implement y_offset
            
            state.OverrideRotX = (bool?) json.SelectToken("override_rot_x") ?? state.OverrideRotX;
            state.RotX = (float?) json.SelectToken("rot_x") ?? state.RotX;
            state.OverrideRotY = (bool?) json.SelectToken("override_rot_y") ?? state.OverrideRotY;
            state.RotY = (float?) json.SelectToken("rot_y") ?? state.RotY;
            state.OverrideRotZ = (bool?) json.SelectToken("override_rot_z") ?? state.OverrideRotZ;
            state.RotZ = (float?) json.SelectToken("rot_z") ?? state.RotZ;
            
            state.OverrideRingColor = (bool?) json.SelectToken("override_ring_color") ?? state.OverrideRingColor;
            if (ColorUtility.TryParseHtmlString((string) json.SelectToken("ring_color"), out var tmp))
                state.RingColor = new Color {R = tmp.r, G = tmp.g, B = tmp.b, A = tmp.a};
            state.OverrideFillColor = (bool?) json.SelectToken("override_fill_color") ?? state.OverrideFillColor;
            if (ColorUtility.TryParseHtmlString((string) json.SelectToken("fill_color"), out var tmp2))
                state.FillColor = new Color {R = tmp2.r, G = tmp2.g, B = tmp2.b, A = tmp2.a};
            state.OpacityMultiplier = (float?) json.SelectToken("opacity_multiplier") ?? state.OpacityMultiplier;
            state.SizeMultiplier = (float?) json.SelectToken("size_multiplier") ?? state.SizeMultiplier;
            state.HoldDirection = (int?) json.SelectToken("hold_direction") ?? state.HoldDirection;
            state.Style = (int?) json.SelectToken("style") ?? state.Style;
        }
    }
}