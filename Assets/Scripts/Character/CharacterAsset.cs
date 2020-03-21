using E7.Introloop;
using UnityEngine;

public class CharacterAsset : MonoBehaviour
{
    public GradientMeshEffect nameGradient;
    public GameObject tachiePrefab; // Don't delete this! This holds a reference to tachie prefab so that it is downloaded when this asset is downloaded.
    public IntroloopAudio musicAudio;
    public ParallaxElement parallaxPrefab;
    public bool mirrorLayout;

    public float mainMenuUpperLeftOverlayAlpha = 0.7f;
    public float mainMenuRightOverlayAlpha = 1f;
    
    public static string GetTachieAssetId(string characterAssetId)
    {
        return characterAssetId + "Tachie";
    }
}

public interface AnimatedCharacter
{
    void OnEnter();
}