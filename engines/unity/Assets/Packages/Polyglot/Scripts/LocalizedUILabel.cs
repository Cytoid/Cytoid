#if NGUI

using System;
using Polyglot;
using UnityEngine;

[AddComponentMenu("UI/Localized UILabel", 14)]
[RequireComponent(typeof(UILabel))]
public class LocalizedUILabel : LocalizedTextComponent<UILabel>
{
    protected override void SetText(UILabel text, string value)
    {
        text.text = value;
    }

    protected override void UpdateAlignment(UILabel text, LanguageDirection direction)
    {
        if (IsOppositeDirection(text.alignment, direction))
        {
            switch (text.alignment)
            {
                case NGUIText.Alignment.Left:
                    text.alignment = NGUIText.Alignment.Right;
                    break;
                case NGUIText.Alignment.Right:
                    text.alignment = NGUIText.Alignment.Left;
                    break;
            }
        }
    }

    private bool IsOppositeDirection(NGUIText.Alignment alignment, LanguageDirection direction)
    {
        return (direction == LanguageDirection.LeftToRight && IsAlignmentRight(alignment)) || (direction == LanguageDirection.RightToLeft && IsAlignmentLeft(alignment));
    }

    private bool IsAlignmentRight(NGUIText.Alignment alignment)
    {
        return alignment == NGUIText.Alignment.Right;
    }
    private bool IsAlignmentLeft(NGUIText.Alignment alignment)
    {
        return alignment == NGUIText.Alignment.Left;
    }
}
#endif
