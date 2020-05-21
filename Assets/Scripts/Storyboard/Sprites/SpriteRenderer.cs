using System;
using UniRx.Async;
using UnityEngine;
using static UnityEngine.Object;

namespace Cytoid.Storyboard.Sprites
{
    public class SpriteRenderer : StoryboardComponentRenderer<Sprite, SpriteState>
    {

        public UnityEngine.UI.Image Image { get; private set; }
        
        public SpriteRenderer(StoryboardRenderer mainRenderer, Sprite component) : base(mainRenderer, component)
        {
        }

        public override StoryboardRendererEaser<SpriteState> CreateEaser() => new SpriteEaser(this);

        public override async UniTask Initialize()
        {
            Image = Instantiate(Provider.SpritePrefab, Provider.Canvas.transform);
            
            Clear();
            
            var spritePath = Component.States[0].Path;
            if (spritePath == null && Component.States.Count > 1) spritePath = Component.States[1].Path;
            if (spritePath == null)
            {
                throw new InvalidOperationException("Sprite does not have a valid path");
            }
            Image.gameObject.name = $"Sprite[{spritePath}]";

            var path = "file://" + MainRenderer.Game.Level.Path + spritePath;
            Image.sprite = await Context.AssetMemory.LoadAsset<UnityEngine.Sprite>(path, AssetTag.Storyboard);
        }

        public override void Clear()
        {
            Image.color = UnityEngine.Color.white.WithAlpha(0);
            Image.preserveAspect = true;
        }

        public override void Dispose()
        {
            Destroy(Image.sprite.texture);
            Destroy(Image.gameObject);
            Image = null;
        }

    }
}