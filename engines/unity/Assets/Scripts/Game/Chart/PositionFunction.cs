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
    /// Sign of display-Y travel as time moves forward (+1 up, -1 down).
    /// Negative <c>a</c> reverses the page, so this is scan × sign(a).
    /// </summary>
    public static int ChronologicalTravelSign(ChartModel.Page page)
    {
        var signA = page.position_arg_a < 0f ? -1 : 1;
        return page.scan_line_direction * signA;
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

        // Affine map of scan progress onto the visible band [d0, d1] for all t
        // (including past-end / pre-page). Equivalent to remapping yRaw∈[b-a, b+a]
        // onto Clamp endpoints; for unclipped a=1,b=0 this is identical to yRaw.
        // Preserves direction so negative a still reverses the page.
        var y0 = b - a;
        var y1 = b + a;
        var d0 = Mathf.Clamp(y0, -1f, 1f);
        var d1 = Mathf.Clamp(y1, -1f, 1f);
        return d0 + (d1 - d0) * t;
    }
}
