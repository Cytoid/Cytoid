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
        
        public SpriteRenderer(StoryboardRenderer mainRenderer, Sprite component) : base(mainRenderer, component)
        {
        }
        
        public override Transform Transform => RectTransform;
        
        public override bool IsOnCanvas => true;

        public override StoryboardRendererEaser<SpriteState> CreateEaser() => new SpriteEaser(this);

        public override async UniTask Initialize()
        {
            var targetRenderer = GetTargetRenderer<SpriteRenderer>();
            if (targetRenderer != null)
            {
                Image = targetRenderer.Image;
                RectTransform = targetRenderer.RectTransform;
                Canvas = targetRenderer.Canvas;
                CanvasGroup = targetRenderer.CanvasGroup;
            }
            else
            {
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

                LoadPath = "file://" + MainRenderer.Game.Level.Path + spritePath;
                Image.sprite = await Context.AssetMemory.LoadAsset<UnityEngine.Sprite>(LoadPath, AssetTag.Storyboard);

                if (!MainRenderer.SpritePathRefCount.ContainsKey(LoadPath))
                    MainRenderer.SpritePathRefCount[LoadPath] = 0;
                MainRenderer.SpritePathRefCount[LoadPath]++;
            }
        }

        public override void Clear()
        {
            Image.color = UnityEngine.Color.white;
            Image.preserveAspect = true;
            CanvasGroup.alpha = 0;
            IsTransformActive = false;
        }

        public override void Dispose()
        {
            if (LoadPath != null && MainRenderer.SpritePathRefCount.ContainsKey(LoadPath))
            {
                MainRenderer.SpritePathRefCount[LoadPath]--;
                if (MainRenderer.SpritePathRefCount[LoadPath] == 0)
                {
                    Context.AssetMemory.DisposeAsset(LoadPath, AssetTag.Storyboard);
                }
            }
            Destroy(Image.gameObject);
            Image = null;
        }

    }
}