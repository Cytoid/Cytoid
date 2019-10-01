using UnityEngine;
using UnityEngine.UI;

namespace Cytoid.Storyboard
{
    public class StoryboardRendererProvider : SingletonMonoBehavior<StoryboardRendererProvider>
    {
        public Camera Camera;
        public Image Cover;
        public Canvas Canvas;
        public CanvasGroup CanvasGroup;
        public RectTransform CanvasRectTransform;
        public Rect CanvasRect => CanvasRectTransform.rect;
        
        public CanvasGroup UiCanvasGroup;
        
        public CameraFilterPack_TV_ARCADE_2 Arcade;
        public CameraFilterPack_TV_Artefact Artifact;
        public CameraFilterPack_TV_Chromatical Chromatical;
        public CameraFilterPack_Color_BrightContrastSaturation ColorAdjustment;
        public CameraFilterPack_Color_RGB ColorFilter;
        public CameraFilterPack_Distortion_Dream Dream;
        public CameraFilterPack_Distortion_FishEye Fisheye;
        public CameraFilterPack_Drawing_Manga_Flash_Color Focus;
        public CameraFilterPack_FX_Glitch1 Glitch;
        public CameraFilterPack_Color_GrayScale GrayScale;
        public CameraFilterPack_Color_Noise Noise;
        public CameraFilterPack_Blur_Radial_Fast RadialBlur;
        public CameraFilterPack_Color_Sepia Sepia;
        public CameraFilterPack_Distortion_ShockWave Shockwave;
        public CameraFilterPack_TV_Videoflip Tape;

        public UnityEngine.UI.Text TextPrefab;
        public Image SpritePrefab;
    }
}