using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Cytoid.Storyboard.Lines
{
    public class LineStateParser : GenericStateParser<LineState>
    {
        public LineStateParser(Storyboard storyboard) : base(storyboard)
        {
        }

        public override void Parse(LineState state, JObject json, LineState baseState)
        {
            ParseObjectState(state, json, baseState);

            json.SelectToken("pos").ToArray().ForEach(it =>
            {
                var pos = new LinePosition();;
                pos.X = ParseNumber(it.SelectToken("x"), ReferenceUnit.NoteX, false) ?? 0;
                pos.Y = ParseNumber(it.SelectToken("y"), ReferenceUnit.NoteY, false) ?? 0;
                state.Pos.Add(pos);
            });
            state.Width = ParseNumber(json.SelectToken("width"), ReferenceUnit.World, false) ?? state.Width;
            if (ColorUtility.TryParseHtmlString((string) json.SelectToken("color"), out var tmp))
                state.Color = new Color {R = tmp.r, G = tmp.g, B = tmp.b, A = tmp.a};
            state.Opacity = (float?) json.SelectToken("opacity") ?? state.Opacity;
            state.Layer = (int?) json.SelectToken("layer") ?? state.Layer;
            state.Order = (int?) json.SelectToken("order") ?? state.Order;
        }
    }
}