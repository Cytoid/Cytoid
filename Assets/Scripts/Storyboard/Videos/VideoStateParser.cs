using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Cytoid.Storyboard.Videos
{
    public class VideoStateParser : GenericStateParser<VideoState>
    {
        public VideoStateParser(Storyboard storyboard) : base(storyboard)
        {
        }

        public override void Parse(VideoState state, JObject json, VideoState baseState)
        {
            ParseCanvasObjectState(state, json, baseState);

            state.Path = (string) json.SelectToken("path") ?? state.Path;
            if (ColorUtility.TryParseHtmlString((string) json.SelectToken("color"), out var tmp))
                state.Color = new Color {R = tmp.r, G = tmp.g, B = tmp.b, A = tmp.a};
        }
    }
}