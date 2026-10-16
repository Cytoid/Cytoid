using UnityEngine;

/// <summary>One segment contract for drag-head motion and its remaining visible line.</summary>
public static class DragPathGeometry
{
    public static float Progress(float time, float fromTime, float toTime)
    {
        var span = toTime - fromTime;
        return span > 0f ? Mathf.Clamp01((time - fromTime) / span) : time >= fromTime ? 1f : 0f;
    }

    public static Vector3 HeadPosition(Vector3 from, Vector3 to, float time, float fromTime, float toTime)
    {
        return Vector3.Lerp(from, to, Progress(time, fromTime, toTime));
    }

    public static Vector3 LineStart(Vector3 visualFrom, Vector3 pathFrom, Vector3 pathTo,
        float time, float fromTime, float toTime)
    {
        return time < fromTime ? visualFrom : HeadPosition(pathFrom, pathTo, time, fromTime, toTime);
    }

    // The line origin has already consumed [0, progress]; don't clip that prefix twice.
    public static float RemainingReveal(float reveal, float progress)
    {
        return progress >= 1f ? 0f : Mathf.Clamp01((reveal - progress) / (1f - progress));
    }

    public static Vector3 Rotation(Vector3 from, Vector3 to, ChartModel.Note.NoteOverride ovr)
    {
        var delta = to - from;
        var rotation = delta.x == 0f && delta.y == 0f
            ? Vector3.zero
            : new Vector3(0f, 0f, -Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg);
        if (ovr.RotX != null) rotation.x = ovr.RotX.Value;
        if (ovr.RotY != null) rotation.y = ovr.RotY.Value;
        if (ovr.RotZ != null) rotation.z = ovr.RotZ.Value;
        return rotation;
    }
}
