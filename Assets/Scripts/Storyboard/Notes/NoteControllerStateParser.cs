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
            state.X = ParseNumber(json.SelectToken("x"), ReferenceUnit.NoteX, false) ?? state.X;
            state.OverrideY = (bool?) json.SelectToken("override_y") ?? state.OverrideY;
            state.Y = ParseNumber(json.SelectToken("y"), ReferenceUnit.NoteY, false) ?? state.Y;
            state.Rot = (float?) json.SelectToken("rot") ?? state.Rot;
            state.OverrideRingColor = (bool?) json.SelectToken("override_color") ?? state.OverrideRingColor;
            if (ColorUtility.TryParseHtmlString((string) json.SelectToken("color"), out var tmp))
                state.RingColor = new Color {R = tmp.r, G = tmp.g, B = tmp.b, A = tmp.a};
            state.OverrideFillColor = (bool?) json.SelectToken("override_color") ?? state.OverrideFillColor;
            if (ColorUtility.TryParseHtmlString((string) json.SelectToken("color"), out var tmp2))
                state.FillColor = new Color {R = tmp2.r, G = tmp2.g, B = tmp2.b, A = tmp2.a};
            state.OpacityMultiplier = (float?) json.SelectToken("opacity_multiplier") ?? state.OpacityMultiplier;
            state.SizeMultiplier = (float?) json.SelectToken("size_multiplier") ?? state.SizeMultiplier;
            state.HoldDirection = (int?) json.SelectToken("hold_direction") ?? state.HoldDirection;
            state.Style = (int?) json.SelectToken("style") ?? state.Style;
        }
    }
}