namespace Cytoid.Storyboard.Controllers
{
    public class ArtifactEaser : StoryboardRendererEaser<ControllerState>
    {
        public ArtifactEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }

        public override void OnUpdate()
        {
            if (!Config.UseEffects) return;
            if (From.Artifact == null) return;

            var effect = Provider.Effects.Artifact;
            effect.Enabled = From.Artifact.Value;
            if (!From.Artifact.Value) return;

            if (From.ArtifactIntensity != null)
                effect.Fade = EaseFloat(From.ArtifactIntensity, To.ArtifactIntensity);
            if (From.ArtifactColorisation != null)
                effect.Colorisation = EaseFloat(From.ArtifactColorisation, To.ArtifactColorisation);
            if (From.ArtifactParasite != null)
                effect.Parasite = EaseFloat(From.ArtifactParasite, To.ArtifactParasite);
            if (From.ArtifactNoise != null)
                effect.Noise = EaseFloat(From.ArtifactNoise, To.ArtifactNoise);
        }
    }
}
