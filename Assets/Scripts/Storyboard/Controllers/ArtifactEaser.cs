namespace Cytoid.Storyboard.Controllers
{
    public class ArtifactEaser : StoryboardRendererEaser<ControllerState>
    {
        public ArtifactEaser()
        {
            Provider.Artifact.Apply(it =>
            {
                it.enabled = false;
                it.Fade = 1;
                it.Colorisation = 1;
                it.Parasite = 1;
                it.Noise = 1;
            });
        }

        public override void OnUpdate()
        {
            if (Config.UseEffects)
            {
                if (From.Artifact.IsSet())
                {
                    Provider.Artifact.enabled = From.Artifact.Value;
                    if (From.Artifact.Value)
                    {
                        if (From.ArtifactIntensity.IsSet())
                        {
                            Provider.Artifact.Fade = EaseFloat(From.ArtifactIntensity, To.ArtifactIntensity);
                        }

                        if (From.ArtifactColorisation.IsSet())
                        {
                            Provider.Artifact.Colorisation =
                                EaseFloat(From.ArtifactColorisation, To.ArtifactColorisation);
                        }

                        if (From.ArtifactParasite.IsSet())
                        {
                            Provider.Artifact.Parasite = EaseFloat(From.ArtifactParasite, To.ArtifactParasite);
                        }

                        if (From.ArtifactNoise.IsSet())
                        {
                            Provider.Artifact.Noise = EaseFloat(From.ArtifactNoise, To.ArtifactNoise);
                        }
                    }
                }
            }
        }
    }
}