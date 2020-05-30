namespace Cytoid.Storyboard.Controllers
{
    public class ArcadeEaser : StoryboardRendererEaser<ControllerState>
    {
        public ArcadeEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (Config.UseEffects)
            {
                if (From.Arcade != null)
                {
                    Provider.Arcade.enabled = From.Arcade.Value;
                    if (From.Arcade.Value)
                    {
                        if (From.ArcadeIntensity != null)
                        {
                            Provider.Arcade.Fade = EaseFloat(From.ArcadeIntensity, To.ArcadeIntensity);
                        }

                        if (From.ArcadeInterferanceSize != null)
                        {
                            Provider.Arcade.Interferance_Size =
                                EaseFloat(From.ArcadeInterferanceSize, To.ArcadeInterferanceSize);
                        }

                        if (From.ArcadeInterferanceSpeed != null)
                        {
                            Provider.Arcade.Interferance_Speed =
                                EaseFloat(From.ArcadeInterferanceSpeed, To.ArcadeInterferanceSpeed);
                        }

                        if (From.ArcadeContrast != null)
                        {
                            Provider.Arcade.Contrast = EaseFloat(From.ArcadeContrast, To.ArcadeContrast);
                        }
                    }
                }
            }
        }
    }
}