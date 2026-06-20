using UnityEngine;

namespace Cytoid.Storyboard.PostProcess
{
    [DisallowMultipleComponent]
    public sealed class StoryboardEffectsHost : MonoBehaviour
    {
        [SerializeField] StoryboardRendererProvider provider;

        void Awake()
        {
            if (provider == null)
                provider = GetComponent<StoryboardRendererProvider>();

            if (StoryboardEffects.Current != null)
                return;

            if (provider != null
                && VendorStoryboardInstall.IsComplete()
                && StoryboardVendorEffectsLoader.TryRegister(provider))
                return;

            var camera = provider != null ? provider.Camera : null;
            if (camera == null)
            {
                Debug.LogError("[StoryboardEffectsHost] No storyboard camera assigned.");
                return;
            }

            var postProcess = camera.GetComponent<StoryboardFallbackPostProcess>();
            if (postProcess == null)
                postProcess = camera.gameObject.AddComponent<StoryboardFallbackPostProcess>();

            postProcess.enabled = true;
            StoryboardEffects.Current = new FallbackStoryboardEffects(postProcess);
        }
    }
}
