using UnityEngine;

/// <summary>
/// C2 PageFunction math (PositionFunction Arguments).
/// Gameplay coordinates only — Storyboard continues to use legacy
/// <see cref="Chart.ConvertChartYToScreenY"/> via <see cref="ChartModel.Note.y"/>.
/// </summary>
public static class PositionFunction
{
    public static void BakeArgs(ChartModel.Page page)
    {
        var pf = page.position_function;
        page.position_arg_a = pf?.Arguments != null && pf.Arguments.Length > 0 ? (float) pf.Arguments[0] : 1f;
        page.position_arg_b = pf?.Arguments != null && pf.Arguments.Length > 1 ? (float) pf.Arguments[1] : 0f;
    }

    public static float GetScanProgress(ChartModel.Page page, float chronologicalT)
    {
        return page.scan_line_direction == 1 ? chronologicalT : (1f - chronologicalT);
    }

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

        var yLow = b - a;
        var yHigh = b + a;

        if (yHigh < -1f)
        {
            low = -1f;
            high = -1f;
            return;
        }

        if (yLow > 1f)
        {
            low = 1f;
            high = 1f;
            return;
        }

        low = Mathf.Max(yLow, -1f);
        high = Mathf.Min(yHigh, 1f);
    }

    /// <summary>
    /// Normalized play-area Y in [-1, 1] (bottom = -1). Scan direction via <see cref="GetScanProgress"/>.
    /// </summary>
    public static float EvaluateDisplayY(ChartModel.Page page, float chronologicalT)
    {
        var a = page.position_arg_a;
        var b = page.position_arg_b;
        if (Mathf.Approximately(a, 0f))
            return Mathf.Clamp(b, -1f, 1f);

        var t = GetScanProgress(page, chronologicalT);
        var yRaw = b + a * (2f * t - 1f);

        if (t < 0f || t > 1f)
            return yRaw;

        var yLow = b - a;
        GetVisibleBand(page, out var low, out var high);
        return low + (high - low) * (yRaw - yLow) / (2f * a);
    }
}
