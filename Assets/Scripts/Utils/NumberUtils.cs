using System;
using System.Globalization;

public static class NumberUtils
{
    /// <summary>
    /// Parse a string to float using InvariantCulture
    /// </summary>
    /// <param name="value">The string to parse</param>
    /// <returns>The parsed float value</returns>
    /// <exception cref="FormatException">Thrown when the string is not in a valid format</exception>
    public static float ParseFloat(string value)
    {
        return float.Parse(value, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Try to parse a string to float using InvariantCulture
    /// </summary>
    /// <param name="value">The string to parse</param>
    /// <param name="result">The parsed float value</param>
    /// <returns>Whether the parsing was successful</returns>
    public static bool TryParseFloat(string value, out float result)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }

    /// <summary>
    /// Parse a string to int using InvariantCulture
    /// </summary>
    /// <param name="value">The string to parse</param>
    /// <returns>The parsed int value</returns>
    /// <exception cref="FormatException">Thrown when the string is not in a valid format</exception>
    public static int ParseInt(string value)
    {
        return int.Parse(value, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Try to parse a string to int using InvariantCulture
    /// </summary>
    /// <param name="value">The string to parse</param>
    /// <param name="result">The parsed int value</param>
    /// <returns>Whether the parsing was successful</returns>
    public static bool TryParseInt(string value, out int result)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }
}
