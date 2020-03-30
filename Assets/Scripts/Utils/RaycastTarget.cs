using UnityEngine.UI;

/*
 * Credits: http://answers.unity.com/answers/1157876/view.html
 */
/// A concrete subclass of the Unity UI `Graphic` class that just skips drawing.
/// Useful for providing a raycast target without actually drawing anything.
public class RaycastTarget : Graphic
{
    public override void SetMaterialDirty()
    {
    }

    public override void SetVerticesDirty()
    {
    }

    /// Probably not necessary since the chain of calls `Rebuild()`->`UpdateGeometry()`->`DoMeshGeneration()`->`OnPopulateMesh()` won't happen; so here really just as a fail-safe.
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
    }
}

#if UNITY_EDITOR

[UnityEditor.CanEditMultipleObjects, UnityEditor.CustomEditor(typeof(RaycastTarget), false)]
public class NonDrawingGraphicEditor : UnityEditor.UI.GraphicEditor
{
    public override void OnInspectorGUI ()
    {
        serializedObject.Update();
        UnityEditor.EditorGUILayout.PropertyField(m_Script);
        // skipping AppearanceControlsGUI
        RaycastControlsGUI();
        serializedObject.ApplyModifiedProperties();
    }
}

#endif