using System.Collections.Generic;
using Cytoid.Storyboard;
using Cytoid.Storyboard.Controllers;

namespace Storyboard.Controllers
{
    public class ControllerEaser : StoryboardRendererEaser<ControllerState>
    {

        private List<StoryboardRendererEaser<ControllerState>> children;
        
        public ControllerEaser(StoryboardRenderer renderer) : base(renderer)
        {
            children = new List<StoryboardRendererEaser<ControllerState>>
            {
                new StoryboardOpacityEaser(renderer),
                new UiOpacityEaser(renderer),
                new ScannerOpacityEaser(renderer),
                new BackgroundDimEaser(renderer),
                new NoteOpacityEaser(renderer),
                new ScannerColorEaser(renderer),
                new ScannerSmoothingEaser(renderer),
                new ScannerPositionEaser(renderer),
                new GlobalNoteRingColorEaser(renderer),
                new GlobalNoteFillColorEaser(renderer),

                new RadialBlurEaser(renderer),
                new ColorAdjustmentEaser(renderer),
                new GrayScaleEaser(renderer),
                new NoiseEaser(renderer),
                new ColorFilterEaser(renderer),
                new SepiaEaser(renderer),
                new DreamEaser(renderer),
                new FisheyeEaser(renderer),
                new ShockwaveEaser(renderer),
                new FocusEaser(renderer),
                new GlitchEaser(renderer),
                new ArtifactEaser(renderer),
                new ArcadeEaser(renderer),
                new ChromaticalEaser(renderer),
                new TapeEaser(renderer),
                new BloomEaser(renderer),

                new CameraEaser(renderer)
            };
        }

        public override void OnUpdate()
        {
            children.ForEach(it =>
            {
                it.From = From;
                it.To = To;
                it.Ease = Ease;
                it.OnUpdate();
            });
        }
    }
}