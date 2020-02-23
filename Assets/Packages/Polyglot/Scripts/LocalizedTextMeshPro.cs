#if TMP_PRESENT
using UnityEngine;
using TMPro;

namespace Polyglot
{
    [AddComponentMenu("UI/Localized TextMesh Pro", 13)]
    [RequireComponent(typeof(TextMeshPro))]
    public class LocalizedTextMeshPro : LocalizedTextComponent<TextMeshPro>
    {
        protected override void SetText(TextMeshPro text, string value)
        {
            text.text = value;
        }

        protected override void UpdateAlignment(TextMeshPro text, LanguageDirection direction)
        {
            if (IsOppositeDirection(text.alignment, direction))
            {
                switch (text.alignment)
                {
                    case TextAlignmentOptions.TopLeft:
                        text.alignment = TextAlignmentOptions.TopRight;
                        break;
                    case TextAlignmentOptions.TopRight:
                        text.alignment = TextAlignmentOptions.TopLeft;
                        break;
                    case TextAlignmentOptions.Left:
                        text.alignment = TextAlignmentOptions.Right;
                        break;
                    case TextAlignmentOptions.Right:
                        text.alignment = TextAlignmentOptions.Left;
                        break;
                    case TextAlignmentOptions.BottomLeft:
                        text.alignment = TextAlignmentOptions.BottomRight;
                        break;
                    case TextAlignmentOptions.BottomRight:
                        text.alignment = TextAlignmentOptions.BottomLeft;
                        break;
                }
            }
        }

        private bool IsOppositeDirection(TextAlignmentOptions alignment, LanguageDirection direction)
        {
            return (direction == LanguageDirection.LeftToRight && IsAlignmentRight(alignment)) || (direction == LanguageDirection.RightToLeft && IsAlignmentLeft(alignment));
        }

        private bool IsAlignmentRight(TextAlignmentOptions alignment)
        {
            return alignment == TextAlignmentOptions.BottomRight || alignment == TextAlignmentOptions.Right || alignment == TextAlignmentOptions.TopRight;
        }
        private bool IsAlignmentLeft(TextAlignmentOptions alignment)
        {
            return alignment == TextAlignmentOptions.BottomLeft || alignment == TextAlignmentOptions.Left || alignment == TextAlignmentOptions.TopLeft;
        }
    }
}

#endif
