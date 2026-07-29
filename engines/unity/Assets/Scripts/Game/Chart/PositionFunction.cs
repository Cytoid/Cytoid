using UnityEngine;

/// <summary>
/// C2 PageFunction math (PositionFunction Arguments).
/// Gameplay coordinates only — Storyboard continues to use legacy
/// <see cref="Chart.ConvertChartYToScreenY"/> via <see cref="ChartModel.Note.y"/>.
/// Negative <c>a</c> reverses the page (scan maps top↔bottom); that is intentional.
/// </summary>
public static class PositionFunction
{
    public static void BakeArgs(ChartModel.Page page)
    {
        var pf = page.position_function;
        page.position_arg_a = pf?.Arguments != null && pf.Arguments.Length > 0 ? (float) pf.Arguments[0] : 1f;
        page.position_arg_b = pf?.Arguments != null && pf.Arguments.Length > 1 ? (float) pf.Arguments[1] : 0f;
    }

    public static float GetScanProgress(int scanLineDirection, float chronologicalT)
    {
        return scanLineDirection == 1 ? chronologicalT : (1f - chronologicalT);
    }

    public static float GetScanProgress(ChartModel.Page page, float chronologicalT)
    {
        return GetScanProgress(page.scan_line_direction, chronologicalT);
    }

    /// <summary>
    /// Visible play-area band in display Y (always <paramref name="low"/> ≤ <paramref name="high"/>).
    /// </summary>
    public static void GetVisibleBand(ChartModel.Page page, out float low, out float high)
    {
        var a = page.position_arg_a;
        var b = page.position_arg_b;
        if (Mathf.Approximately(a, 0f))
        {
            var y = Mathf.Clamp(b, -1f, 1f);
            low = y;
            high = y;
            return;
        }

        var y0 = b - a;
        var y1 = b + a;
        var sortedLo = Mathf.Min(y0, y1);
        var sortedHi = Mathf.Max(y0, y1);

        if (sortedHi < -1f)
        {
            low = -1f;
            high = -1f;
            return;
        }

        if (sortedLo > 1f)
        {
            low = 1f;
            high = 1f;
            return;
        }

        low = Mathf.Max(sortedLo, -1f);
        high = Mathf.Min(sortedHi, 1f);
    }

    /// <summary>
    /// Normalized play-area Y in [-1, 1] (bottom = -1).
    /// </summary>
    public static float EvaluateDisplayY(ChartModel.Page page, float chronologicalT)
    {
        return EvaluateDisplayY(page, chronologicalT, page.scan_line_direction);
    }

    /// <summary>
    /// Same as <see cref="EvaluateDisplayY(ChartModel.Page, float)"/> with an explicit scan direction
    /// (used for legacy past-end extrapolation that mirrors with a flipped direction).
    /// </summary>
    public static float EvaluateDisplayY(ChartModel.Page page, float chronologicalT, int scanLineDirection)
    {
        var a = page.position_arg_a;
        var b = page.position_arg_b;
        if (Mathf.Approximately(a, 0f))
            return Mathf.Clamp(b, -1f, 1f);

        var t = GetScanProgress(scanLineDirection, chronologicalT);
        var yRaw = b + a * (2f * t - 1f);

        // Outside the page chronologically (or the mirrored past-end segment): raw linear.
        if (t < 0f || t > 1f)
            return yRaw;

        // Remap the raw segment [b-a, b+a] onto its intersection with [-1, 1],
        // preserving direction so negative a still reverses the page.
        var y0 = b - a;
        var y1 = b + a;
        var d0 = Mathf.Clamp(y0, -1f, 1f);
        var d1 = Mathf.Clamp(y1, -1f, 1f);
        return d0 + (d1 - d0) * (yRaw - y0) / (y1 - y0);
    }
}
