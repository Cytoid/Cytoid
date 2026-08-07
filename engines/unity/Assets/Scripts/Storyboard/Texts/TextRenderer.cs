using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Object;

namespace Cytoid.Storyboard.Texts
{
    public class TextRenderer : StoryboardComponentRenderer<Text, TextState>
    {
        
        public UnityEngine.UI.Text Text { get; private set; }
        
        public LetterSpacing LetterSpacing { get; private set; }
        
        public RectTransform RectTransform { get; private set; }
        
        public Canvas Canvas { get; private set; }
        
        public CanvasGroup CanvasGroup { get; private set; }

        private bool ownsResources;

        public TextRenderer(StoryboardRenderer mainRenderer, Text component) : base(mainRenderer, component)
        {
        }

        public override Transform Transform => RectTransform;

        public override bool IsOnCanvas => true;

        public override async UniTask Initialize()
        {
            BeginInitialize();
            var targetRenderer = GetTargetRenderer<TextRenderer>();
            if (targetRenderer != null)
            {
                ownsResources = false;
                Text = targetRenderer.Text;
                LetterSpacing = targetRenderer.LetterSpacing;
                RectTransform = targetRenderer.RectTransform;
                Canvas = targetRenderer.Canvas;
                CanvasGroup = targetRenderer.CanvasGroup;
            }
            else
            {
                ownsResources = true;
                Text = Instantiate(Provider.TextPrefab, GetParentTransform());
                RectTransform = Text.rectTransform;
                LetterSpacing = Text.GetComponent<LetterSpacing>();
                LetterSpacing.enabled = false;
                Canvas = Text.GetComponent<Canvas>();
                Canvas.overrideSorting = true;
                Canvas.sortingLayerName = "Storyboard1";
                CanvasGroup = Text.GetComponent<CanvasGroup>();
                Text.gameObject.name = $"Text[{Component.States[0].Text}]";
                Clear();
            }
        }

        public override StoryboardRendererEaser<TextState> CreateEaser() => new TextEaser(this);
        
        public override void Clear()
        {
            if (Text != null)
            {
                Text.text = "";
                Text.fontSize = 20;
                Text.alignment = TextAnchor.MiddleCenter;
                Text.color = UnityEngine.Color.white;
            }
            if (LetterSpacing != null)
            {
                LetterSpacing.enabled = false;
                LetterSpacing.Spacing = 0;
            }
            if (CanvasGroup != null)
                CanvasGroup.alpha = 0;
            IsTransformActive = false;
        }

        public override void Dispose()
        {
            if (IsDisposed) return;
            if (ownsResources && Text != null)
                Destroy(Text.gameObject);
            Text = null;
            LetterSpacing = null;
            RectTransform = null;
            Canvas = null;
            CanvasGroup = null;
            ownsResources = false;
            base.Dispose();
        }

    }
}
