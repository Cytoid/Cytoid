using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using static UnityEngine.Object;

namespace Cytoid.Storyboard.Sprites
{
    public class SpriteRenderer : StoryboardComponentRenderer<Sprite, SpriteState>
    {

        public UnityEngine.UI.Image Image { get; private set; }

        public RectTransform RectTransform { get; private set; }

        public Canvas Canvas { get; private set; }

        public CanvasGroup CanvasGroup { get; private set; }

        public string LoadPath { get; private set; }

        private bool ownsResources;
        private bool holdsAssetRef;

        public SpriteRenderer(StoryboardRenderer mainRenderer, Sprite component) : base(mainRenderer, component)
        {
        }

        public override Transform Transform => RectTransform;

        public override bool IsOnCanvas => true;

        public override StoryboardRendererEaser<SpriteState> CreateEaser() => new SpriteEaser(this);

        public override async UniTask Initialize()
        {
            var version = BeginInitialize();
            var targetRenderer = GetTargetRenderer<SpriteRenderer>();
            if (targetRenderer != null)
            {
                ownsResources = false;
                Image = targetRenderer.Image;
                RectTransform = targetRenderer.RectTransform;
                Canvas = targetRenderer.Canvas;
                CanvasGroup = targetRenderer.CanvasGroup;
            }
            else
            {
                ownsResources = true;
                Image = Instantiate(Provider.SpritePrefab, GetParentTransform());
                RectTransform = Image.rectTransform;
                Canvas = Image.GetComponent<Canvas>();
                Canvas.overrideSorting = true;
                Canvas.sortingLayerName = "Storyboard1";
                CanvasGroup = Image.GetComponent<CanvasGroup>();

                Clear();

                var spritePath = Component.States[0].Path;
                if (spritePath == null && Component.States.Count > 1) spritePath = Component.States[1].Path;
                if (spritePath == null)
                {
                    throw new InvalidOperationException("Sprite does not have a valid path");
                }
                Image.gameObject.name = $"Sprite[{spritePath}]";

                var loadPath = MainRenderer.Game.UsesExternalContent
                    ? GameLaunchVfs.ResolveRequiredFileUri(
                        MainRenderer.Game.Level.Path,
                        spritePath,
                        "storyboard.sprite.path")
                    : "file://" + MainRenderer.Game.Level.Path + spritePath;
                LoadPath = loadPath;
                // Capture before await: Dispose may null MainRenderer / clear fields mid-load.
                var mainRenderer = MainRenderer;
                var sprite = await Context.AssetMemory.LoadAsset<UnityEngine.Sprite>(loadPath, AssetTag.Storyboard);

                if (IsInitializeStale(version) || Image == null)
                {
                    ReleaseUnusedLoadedAsset(loadPath, mainRenderer);
                    return;
                }

                Image.sprite = sprite;

                if (!mainRenderer.SpritePathRefCount.ContainsKey(loadPath))
                    mainRenderer.SpritePathRefCount[loadPath] = 0;
                mainRenderer.SpritePathRefCount[loadPath]++;
                holdsAssetRef = true;
            }
        }

        public override void Clear()
        {
            if (Image != null)
            {
                Image.color = UnityEngine.Color.white;
                Image.preserveAspect = true;
            }
            if (CanvasGroup != null)
                CanvasGroup.alpha = 0;
            IsTransformActive = false;
        }

        public override void Dispose()
        {
            if (IsDisposed) return;
            ReleaseAssetRef();
            if (ownsResources && Image != null)
                Destroy(Image.gameObject);
            Image = null;
            RectTransform = null;
            Canvas = null;
            CanvasGroup = null;
            ownsResources = false;
            base.Dispose();
        }

        private void ReleaseAssetRef()
        {
            if (!holdsAssetRef || LoadPath == null || MainRenderer == null) return;
            if (MainRenderer.SpritePathRefCount.ContainsKey(LoadPath))
            {
                MainRenderer.SpritePathRefCount[LoadPath]--;
                if (MainRenderer.SpritePathRefCount[LoadPath] <= 0)
                {
                    MainRenderer.SpritePathRefCount.Remove(LoadPath);
                    Context.AssetMemory.DisposeAsset(LoadPath, AssetTag.Storyboard);
                }
            }
            holdsAssetRef = false;
        }

        private static void ReleaseUnusedLoadedAsset(string loadPath, StoryboardRenderer mainRenderer)
        {
            // Stale completion: LoadAsset already pinned the cache entry, but no business ref was taken.
            if (string.IsNullOrEmpty(loadPath)) return;
            if (mainRenderer != null &&
                mainRenderer.SpritePathRefCount.TryGetValue(loadPath, out var count) &&
                count > 0)
            {
                return;
            }

            Context.AssetMemory.DisposeAsset(loadPath, AssetTag.Storyboard);
        }

    }
}
