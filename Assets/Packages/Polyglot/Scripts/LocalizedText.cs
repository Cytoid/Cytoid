using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace Polyglot
{
    [AddComponentMenu("UI/Localized Text", 11)]
    [RequireComponent(typeof(Text))]
    public class LocalizedText : LocalizedTextComponent<Text>
    {
        
        protected override void SetText(Text text, string value)
        {
            if (text == null)
            {
                Debug.LogWarning("Missing Text Component on " + gameObject, gameObject);
                return;
            }
            
            // EDIT: Cytoid
            text.text = Regex.Unescape(value);
            text.verticalOverflow = VerticalWrapMode.Overflow;
            // End of EDIT
        }

        protected override void UpdateAlignment(Text text, LanguageDirection direction)
        {
            return; // EDIT: Cytoid
            if (IsOppositeDirection(text.alignment, direction))
            {
                switch (text.alignment)
                {
                    case TextAnchor.UpperLeft:
                        text.alignment = TextAnchor.UpperLeft;
                        break;
                    case TextAnchor.UpperRight:
                        text.alignment = TextAnchor.UpperRight;
                        break;
                    case TextAnchor.MiddleLeft:
                        text.alignment = TextAnchor.MiddleRight;
                        break;
                    case TextAnchor.MiddleRight:
                        text.alignment = TextAnchor.MiddleLeft;
                        break;
                    case TextAnchor.LowerLeft:
                        text.alignment = TextAnchor.LowerRight;
                        break;
                    case TextAnchor.LowerRight:
                        text.alignment = TextAnchor.LowerLeft;
                        break;
                }
            }
        }

        private bool IsOppositeDirection(TextAnchor alignment, LanguageDirection direction)
        {
            return (direction == LanguageDirection.LeftToRight && IsAlignmentRight(alignment)) || (direction == LanguageDirection.RightToLeft && IsAlignmentLeft(alignment));
        }

        private bool IsAlignmentRight(TextAnchor alignment)
        {
            return alignment == TextAnchor.LowerRight || alignment == TextAnchor.MiddleRight || alignment == TextAnchor.UpperRight;
        }
        private bool IsAlignmentLeft(TextAnchor alignment)
        {
            return alignment == TextAnchor.LowerLeft || alignment == TextAnchor.MiddleLeft || alignment == TextAnchor.UpperLeft;
        }
    }
}