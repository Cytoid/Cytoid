using E7.Introloop;
using UnityEngine;

public class CharacterAsset : MonoBehaviour
{
    public GradientMeshEffect nameGradient;
     public IntroloopAudio musicAudio;
    public ParallaxElement parallaxPrefab;
    public bool mirrorLayout;

    public float mainMenuUpperLeftOverlayAlpha = 0.7f;
    public float mainMenuRightOverlayAlpha = 1f;
    
    public static string GetMainBundleId(string id)
    {
        return "character_" + id.ToLower();
    }
    
    public static string GetTachieBundleId(string id)
    {
        return "character_" + id.ToLower() + "_tachie";
    }
}

public interface AnimatedCharacter
{
    void OnEnter();
}