using System.Collections.Generic;
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

            if (json.SelectToken("pos") != null)
            {
                state.Pos = new List<LinePosition>();
                json.SelectToken("pos").ToArray().ForEach(it =>
                {
                    var pos = new LinePosition
                    {
                        X = ParseUnitFloat(it.SelectToken("x"), ReferenceUnit.NoteX, false, false, 0),
                        Y = ParseUnitFloat(it.SelectToken("y"), ReferenceUnit.NoteY, false, false, 0),
                        Z = ParseUnitFloat(it.SelectToken("z"), ReferenceUnit.World, false, false, 0)
                    };
                    state.Pos.Add(pos);
                });
            }
            state.Width = ParseUnitFloat(json.SelectToken("width"), ReferenceUnit.World, false, true) ?? state.Width;
            if (ColorUtility.TryParseHtmlString((string) json.SelectToken("color"), out var tmp))
                state.Color = new Color {R = tmp.r, G = tmp.g, B = tmp.b, A = tmp.a};
            state.Opacity = (float?) json.SelectToken("opacity") ?? state.Opacity;
            state.Layer = (int?) json.SelectToken("layer") ?? state.Layer;
            state.Order = (int?) json.SelectToken("order") ?? state.Order;
        }
    }
}