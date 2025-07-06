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
    /// Parse a string to int using InvariantCulture
    /// </summary>
    /// <param name="value">The string to parse</param>
    /// <returns>The parsed int value</returns>
    /// <exception cref="FormatException">Thrown when the string is not in a valid format</exception>
    public static int ParseInt(string value)
    {
        return int.Parse(value, CultureInfo.InvariantCulture);
    }
}
