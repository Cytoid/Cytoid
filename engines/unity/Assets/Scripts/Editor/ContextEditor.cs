using System;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

[CustomEditor(typeof(Context))]
public class ContextEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (Application.isPlaying)
        {
            if (Context.ScreenManager != null)
            {
                GUILayout.Label("Screen history:", new GUIStyle().Also(it => it.fontStyle = FontStyle.Bold));
                foreach (var intent in Context.ScreenManager.History)
                {
                    GUILayout.Label(intent.ScreenId);
                }
                GUILayout.Label("");
            }

            if (Context.AssetMemory != null)
            {
                GUILayout.Label("Asset memory usage:", new GUIStyle().Also(it => it.fontStyle = FontStyle.Bold));
                foreach (AssetTag tag in Enum.GetValues(typeof(AssetTag)))
                {
                    GUILayout.Label(
                        $"{tag}: {Context.AssetMemory.CountTagUsage(tag)}/{(Context.AssetMemory.GetTagLimit(tag) > 0 ? Context.AssetMemory.GetTagLimit(tag).ToString() : "∞")}");
                }
                GUILayout.Label("");
            }

            if (GUILayout.Button("Unload unused assets"))
            {
                Resources.UnloadUnusedAssets();
            }

            EditorUtility.SetDirty(target);
        }
    }
}
#endif
