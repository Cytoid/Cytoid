using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Cytoid.Storyboard.Texts
{
    public class TextStateParser : GenericStateParser<TextState>
    {
        public TextStateParser(Storyboard storyboard) : base(storyboard)
        {
        }

        public override void Parse(TextState state, JObject json, TextState baseState)
        {
            ParseStageObjectState(state, json, baseState);

            state.Font = (string) json.SelectToken("font") ?? state.Font;
            if (ColorUtility.TryParseHtmlString((string) json.SelectToken("color"), out var tmp))
                state.Color = new Color {R = tmp.r, G = tmp.g, B = tmp.b, A = tmp.a};
            state.Text = (string) json.SelectToken("text") ?? state.Text;
            state.Size = (int?) json.SelectToken("size") ?? state.Size;
            state.Align = (string) json.SelectToken("align") ?? state.Align;
            state.LetterSpacing = (float?) json.SelectToken("letter_spacing") ?? state.LetterSpacing;
            state.FontWeight = json["font_weight"] != null
                ? (FontWeight) Enum.Parse(typeof(FontWeight), (string) json["font_weight"], true)
                : (FontWeight?) null;
        }
    }
}