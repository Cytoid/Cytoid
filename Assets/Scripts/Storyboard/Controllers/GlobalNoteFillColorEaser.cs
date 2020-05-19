using System.Linq;

namespace Cytoid.Storyboard.Controllers
{
    public class GlobalNoteFillColorEaser : StoryboardRendererEaser<ControllerState>
    {
        public override void OnUpdate()
        {
            if (From.NoteFillColors.IsSet())
            {
                foreach (var (key, value) in GameConfig.NoteColorChartOverrideMapping.Select(it => (it.Key, it.Value)))
                {
                    Game.Config.GlobalFillColorsOverride[key] = new []{
                        EaseColor(From.NoteFillColors[value[0]], To.NoteFillColors[value[0]]),
                        EaseColor(From.NoteFillColors[value[1]], To.NoteFillColors[value[1]])
                    };
                }
            }
        }
    }
}