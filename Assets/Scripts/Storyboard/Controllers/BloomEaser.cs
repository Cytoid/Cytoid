using SleekRender;

namespace Cytoid.Storyboard.Controllers
{
    public class BloomEaser : StoryboardRendererEaser<ControllerState>
    {
        private SleekRenderPostProcess sleek;
        
        public BloomEaser()
        {
            sleek = Provider.SleekRender;
            sleek.Apply(it =>
            {
                it.enabled = false;
                it.settings.bloomEnabled = false;
                it.settings.bloomIntensity = 0;
            });
        }

        public override void OnUpdate()
        {
            if (Config.UseEffects)
            {
                if (From.Bloom.IsSet())
                {
                    sleek.enabled = sleek.settings.bloomEnabled = From.Bloom.Value;
                    if (From.Bloom.Value)
                    {
                        if (From.BloomIntensity.IsSet())
                        {
                            sleek.settings.bloomIntensity = EaseFloat(From.BloomIntensity, To.BloomIntensity);
                        }
                    }
                }
            }
        }
    }
}