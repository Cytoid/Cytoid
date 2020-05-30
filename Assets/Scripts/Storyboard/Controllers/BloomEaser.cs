using SleekRender;

namespace Cytoid.Storyboard.Controllers
{
    public class BloomEaser : StoryboardRendererEaser<ControllerState>
    {
        private readonly SleekRenderPostProcess sleek;
        
        public BloomEaser(StoryboardRenderer renderer) : base(renderer)
        {
            sleek = Provider.SleekRender;
        }

        public override void OnUpdate()
        {
            if (Config.UseEffects)
            {
                if (From.Bloom != null)
                {
                    sleek.enabled = sleek.settings.bloomEnabled = From.Bloom.Value;
                    if (From.Bloom.Value)
                    {
                        if (From.BloomIntensity != null)
                        {
                            sleek.settings.bloomIntensity = EaseFloat(From.BloomIntensity, To.BloomIntensity);
                        }
                    }
                }
            }
        }
    }
}