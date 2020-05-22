using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Cytoid.Storyboard.Sprites
{
    public class SpriteStateParser : GenericStateParser<SpriteState>
    {
        public SpriteStateParser(Storyboard storyboard) : base(storyboard)
        {
        }

        public override void Parse(SpriteState state, JObject json, SpriteState baseState)
        {
            ParseStageObjectState(state, json, baseState);

            state.Path = (string) json.SelectToken("path") ?? state.Path;
            state.PreserveAspect = (bool?) json.SelectToken("preserve_aspect") ?? state.PreserveAspect;
            if (ColorUtility.TryParseHtmlString((string) json.SelectToken("color"), out var tmp))
                state.Color = new Color {R = tmp.r, G = tmp.g, B = tmp.b, A = tmp.a};
        }
    }
}