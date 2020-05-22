using UniRx.Async;
using UnityEngine;
using static UnityEngine.Object;

namespace Cytoid.Storyboard.Texts
{
    public class TextRenderer : StageObjectRenderer<Text, TextState>
    {
        
        public UnityEngine.UI.Text Text { get; private set; }
        
        public TextRenderer(StoryboardRenderer mainRenderer, Text component) : base(mainRenderer, component)
        {
        }
        
        public override async UniTask Initialize()
        {
            Text = Instantiate(Provider.TextPrefab, Provider.Canvas.transform);
            Text.gameObject.name = $"Text[{Component.States[0].Text}]";
            Clear();
        }

        public override StoryboardRendererEaser<TextState> CreateEaser() => new TextEaser(this);
        
        public override void Clear()
        {
            Text.text = "";
            Text.fontSize = 20;
            Text.alignment = TextAnchor.MiddleCenter;
            Text.color = UnityEngine.Color.white.WithAlpha(0);
        }

        public override void Dispose()
        {
            Destroy(Text.gameObject);
            Text = null;
        }
        
    }
}