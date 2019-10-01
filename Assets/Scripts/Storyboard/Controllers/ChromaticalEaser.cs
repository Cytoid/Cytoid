namespace Cytoid.Storyboard.Controllers
{
    public class ChromaticalEaser : StoryboardRendererEaser<ControllerState>
    {
        public ChromaticalEaser()
        {
            Provider.Chromatical.Apply(it =>
            {
                it.enabled = false;
                it.Fade = 1;
                it.Intensity = 1;
                it.Speed = 1;
            });
        }

        public override void OnUpdate()
        {
            if (Config.UseEffects)
            {
                if (From.Chromatical.IsSet())
                {
                    Provider.Chromatical.enabled = From.Chromatical.Value;
                    if (From.Chromatical.Value)
                    {
                        if (From.ChromaticalFade.IsSet())
                            Provider.Chromatical.Fade = EaseFloat(From.ChromaticalFade, To.ChromaticalFade);

                        if (From.ChromaticalIntensity.IsSet())
                            Provider.Chromatical.Intensity =
                                EaseFloat(From.ChromaticalIntensity, To.ChromaticalIntensity);

                        if (From.ChromaticalSpeed.IsSet())
                            Provider.Chromatical.Speed = EaseFloat(From.ChromaticalSpeed, To.ChromaticalSpeed);
                    }
                    else
                    {
                        Provider.Chromatical.SetTimeX(1.0f);
                    }
                }
            }
        }
    }
}