using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// Sanitizes untrusted TextMesh Pro rich text while preserving a small formatting whitelist.
/// Unsupported and malformed tags are removed while their surrounding text is preserved.
/// </summary>
public static class TmpRichTextSanitizer
{
    public const float DefaultBaseFontSize = 28f;
    public const float MinFontSize = 8f;
    public const float MaxFontSize = 72f;
    public const float MinPercentage = 50f;
    public const float MaxPercentage = 250f;
    public const float MinEm = 0.5f;
    public const float MaxEm = 2.5f;
    public const int MaxNestingDepth = 8;
    public const int MaxInputLength = 4096;
    public const int MaxTextLength = 512;

    private static readonly HashSet<string> SimpleTags = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase)
    {
        "b", "i", "u", "s"
    };

    private static readonly HashSet<string> NamedColors = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase)
    {
        "red", "green", "blue", "yellow", "orange",
        "black", "white", "purple", "grey", "lightblue"
    };

    public static string Sanitize(string input, float baseFontSize = DefaultBaseFontSize)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        if (!IsFinitePositive(baseFontSize)) baseFontSize = DefaultBaseFontSize;

        var inputLength = Math.Min(input.Length, MaxInputLength);
        if (inputLength > 0 && inputLength < input.Length &&
            char.IsHighSurrogate(input[inputLength - 1]))
            inputLength--;

        var output = new StringBuilder(Math.Min(inputLength, MaxTextLength));
        var openTags = new List<OpenTag>();
        var emittedDepth = 0;
        var textLength = 0;

        for (var index = 0; index < inputLength && textLength < MaxTextLength;)
        {
            if (input[index] != '<')
            {
                AppendTextCharacter(input, ref index, inputLength, output, ref textLength);
                continue;
            }

            var endIndex = FindTagEnd(input, index + 1, inputLength);
            if (endIndex < 0)
            {
                index++;
                continue;
            }

            var token = input.Substring(index, endIndex - index + 1);
            index = endIndex + 1;
            if (!TryReadTag(token, out var isClosing, out var name, out var remainder) ||
                !IsSupportedTag(name))
                continue;

            if (isClosing)
            {
                if (!string.IsNullOrWhiteSpace(remainder) || openTags.Count == 0 ||
                    !string.Equals(openTags[openTags.Count - 1].Name, name,
                        StringComparison.OrdinalIgnoreCase))
                    continue;

                var openTag = openTags[openTags.Count - 1];
                openTags.RemoveAt(openTags.Count - 1);
                if (!openTag.Emitted) continue;

                output.Append("</").Append(openTag.Name).Append('>');
                emittedDepth--;
                continue;
            }

            if (!TryNormalizeOpeningTag(name, remainder, baseFontSize, out var normalizedTag))
            {
                // Recognized tag with an invalid value: remove the wrapper while preserving text.
                openTags.Add(new OpenTag(name, false));
                continue;
            }

            if (emittedDepth >= MaxNestingDepth)
            {
                openTags.Add(new OpenTag(name, false));
                continue;
            }

            output.Append(normalizedTag);
            openTags.Add(new OpenTag(name, true));
            emittedDepth++;
        }

        for (var index = openTags.Count - 1; index >= 0; index--)
        {
            if (!openTags[index].Emitted) continue;
            output.Append("</").Append(openTags[index].Name).Append('>');
        }

        return output.ToString();
    }

    private static bool IsSupportedTag(string name) =>
        SimpleTags.Contains(name) ||
        string.Equals(name, "size", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, "color", StringComparison.OrdinalIgnoreCase);

    private static bool TryNormalizeOpeningTag(
        string name,
        string remainder,
        float baseFontSize,
        out string normalizedTag)
    {
        var normalizedName = name.ToLowerInvariant();
        if (SimpleTags.Contains(normalizedName))
        {
            normalizedTag = $"<{normalizedName}>";
            return string.IsNullOrWhiteSpace(remainder);
        }

        if (!TryReadValue(remainder, out var value))
        {
            normalizedTag = string.Empty;
            return false;
        }

        if (normalizedName == "color")
        {
            if (!IsColor(value))
            {
                normalizedTag = string.Empty;
                return false;
            }

            normalizedTag = $"<color={value}>";
            return true;
        }

        if (!TryResolveFontSize(value, baseFontSize, out var fontSize))
        {
            normalizedTag = string.Empty;
            return false;
        }

        normalizedTag = $"<size={fontSize.ToString("0.###", CultureInfo.InvariantCulture)}>";
        return true;
    }

    private static bool TryResolveFontSize(string value, float baseFontSize, out float fontSize)
    {
        var unit = SizeUnit.Pixels;
        var numberText = value;
        if (value.EndsWith("%", StringComparison.Ordinal))
        {
            unit = SizeUnit.Percentage;
            numberText = value.Substring(0, value.Length - 1);
        }
        else if (value.EndsWith("em", StringComparison.OrdinalIgnoreCase))
        {
            unit = SizeUnit.Em;
            numberText = value.Substring(0, value.Length - 2);
        }

        if (!float.TryParse(numberText, NumberStyles.Float, CultureInfo.InvariantCulture,
                out var number) || !IsFinitePositive(Math.Abs(number)))
        {
            fontSize = 0;
            return false;
        }

        switch (unit)
        {
            case SizeUnit.Percentage:
                if (number <= 0)
                {
                    fontSize = 0;
                    return false;
                }
                number = Clamp(number, MinPercentage, MaxPercentage);
                fontSize = baseFontSize * number / 100f;
                break;
            case SizeUnit.Em:
                if (number <= 0)
                {
                    fontSize = 0;
                    return false;
                }
                number = Clamp(number, MinEm, MaxEm);
                fontSize = baseFontSize * number;
                break;
            default:
                fontSize = numberText.StartsWith("+", StringComparison.Ordinal) ||
                           numberText.StartsWith("-", StringComparison.Ordinal)
                    ? baseFontSize + number
                    : number;
                if (fontSize <= 0) return false;
                break;
        }

        fontSize = Clamp(fontSize, MinFontSize, MaxFontSize);
        return true;
    }

    private static bool TryReadValue(string remainder, out string value)
    {
        remainder = remainder.Trim();
        if (remainder.Length < 2 || remainder[0] != '=')
        {
            value = string.Empty;
            return false;
        }

        remainder = remainder.Substring(1).Trim();
        if (remainder.Length == 0)
        {
            value = string.Empty;
            return false;
        }

        if (remainder[0] == '"' || remainder[0] == '\'')
        {
            var quote = remainder[0];
            if (remainder.Length < 3 || remainder[remainder.Length - 1] != quote)
            {
                value = string.Empty;
                return false;
            }
            value = remainder.Substring(1, remainder.Length - 2);
        }
        else
        {
            value = remainder;
        }

        return value.Length > 0 && value.IndexOfAny(new[] {' ', '\t', '\r', '\n'}) < 0;
    }

    private static bool IsColor(string value)
    {
        if (NamedColors.Contains(value)) return true;
        if (value.Length != 4 && value.Length != 5 && value.Length != 7 && value.Length != 9)
            return false;
        if (value[0] != '#') return false;
        for (var index = 1; index < value.Length; index++)
        {
            var c = value[index];
            if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') ||
                  (c >= 'A' && c <= 'F'))) return false;
        }
        return true;
    }

    private static bool TryReadTag(
        string token,
        out bool isClosing,
        out string name,
        out string remainder)
    {
        var content = token.Substring(1, token.Length - 2).Trim();
        isClosing = content.StartsWith("/", StringComparison.Ordinal);
        if (isClosing) content = content.Substring(1).TrimStart();

        var nameLength = 0;
        while (nameLength < content.Length &&
               (char.IsLetter(content[nameLength]) || content[nameLength] == '-'))
            nameLength++;

        if (nameLength == 0)
        {
            name = string.Empty;
            remainder = string.Empty;
            return false;
        }

        name = content.Substring(0, nameLength).ToLowerInvariant();
        remainder = content.Substring(nameLength);
        return true;
    }

    private static int FindTagEnd(string input, int startIndex, int inputLength)
    {
        var quote = '\0';
        for (var index = startIndex; index < inputLength; index++)
        {
            var c = input[index];
            if (quote != '\0')
            {
                if (c == quote) quote = '\0';
                continue;
            }
            if (c == '"' || c == '\'') quote = c;
            else if (c == '>') return index;
        }
        return -1;
    }

    private static void AppendTextCharacter(
        string input,
        ref int index,
        int inputLength,
        StringBuilder output,
        ref int textLength)
    {
        if (char.IsHighSurrogate(input[index]) && index + 1 < inputLength &&
            char.IsLowSurrogate(input[index + 1]))
        {
            if (textLength + 2 > MaxTextLength)
            {
                textLength = MaxTextLength;
                return;
            }
            output.Append(input[index++]).Append(input[index++]);
            textLength += 2;
            return;
        }

        output.Append(input[index++]);
        textLength++;
    }

    private static bool IsFinitePositive(float value) =>
        value > 0 && !float.IsNaN(value) && !float.IsInfinity(value);

    private static float Clamp(float value, float min, float max) =>
        Math.Max(min, Math.Min(max, value));

    private readonly struct OpenTag
    {
        public string Name { get; }
        public bool Emitted { get; }

        public OpenTag(string name, bool emitted)
        {
            Name = name;
            Emitted = emitted;
        }
    }

    private enum SizeUnit
    {
        Pixels,
        Percentage,
        Em
    }
}
