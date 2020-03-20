using UnityEditor;
using UnityEngine;

public class RectTransformInspector : MonoBehaviour
{
}

#if UNITY_EDITOR
[CustomEditor(typeof(RectTransformInspector))]
public class RectTransformEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var rectTransform = ((RectTransformInspector) target).GetComponent<RectTransform>();
        var rect = rectTransform.rect;
        GUILayout.Label($"Local space: x={rect.x}, y={rect.y}, width={rect.width}, height={rect.height}");
        rect = rectTransform.GetScreenSpaceRect();
        GUILayout.Label($"Screen space: x={rect.x}, y={rect.y}, width={rect.width}, height={rect.height}");
        var center = rectTransform.GetScreenSpaceCenter();
        var bounds = rectTransform.GetScreenSpaceBounds();
        GUILayout.Label($"Screen space: centerX={center.x}, centerY={center.y}, minX={bounds.min.x}, maxX={bounds.max.x}, minY={bounds.min.y}, maxY={bounds.max.y}");
        
        EditorUtility.SetDirty(target);
    }
}
#endif