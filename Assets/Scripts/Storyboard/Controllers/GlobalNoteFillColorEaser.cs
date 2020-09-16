using System;
using System.Linq;
using UnityEngine;

namespace Cytoid.Storyboard.Controllers
{
    public class GlobalNoteFillColorEaser : StoryboardRendererEaser<ControllerState>
    {

        public GlobalNoteFillColorEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }
        
        public override void OnUpdate()
        {
            if (From.NoteFillColors != null)
            {
                foreach (var (key, value) in GameConfig.NoteColorChartOverrideMapping.Select(it => (it.Key, it.Value)))
                {
                    var min = Math.Min(value[0], value[1]);
                    if (From.NoteFillColors.Count <= min || To.NoteFillColors == null || To.NoteFillColors.Count <= min) continue;
                    Game.Config.GlobalFillColorsOverride[key] = new []{
                        EaseColor(From.NoteFillColors[value[0]], To.NoteFillColors[value[0]]),
                        EaseColor(From.NoteFillColors[value[1]], To.NoteFillColors[value[1]])
                    };
                }
            }
        }
    }
}