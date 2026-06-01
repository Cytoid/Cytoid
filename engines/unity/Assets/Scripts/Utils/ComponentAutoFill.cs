using UnityEngine;

/// <summary>
/// Extension methods for auto-filling serialized component references.
/// Use in OnValidate() (editor) and Awake() (runtime fallback for prefab instantiation).
/// </summary>
public static class ComponentAutoFill
{
    public static void AutoFill<T>(this Component self, ref T field) where T : Component
    {
        if (field == null) field = self.GetComponent<T>();
    }

    public static void AutoFill<T>(this GameObject self, ref T field) where T : Component
    {
        if (field == null) field = self.GetComponent<T>();
    }

    public static void AutoFillInChildren<T>(this Component self, ref T field, bool includeInactive = true) where T : Component
    {
        if (field == null) field = self.GetComponentInChildren<T>(includeInactive);
    }

    public static void AutoFillInChildrenByName<T>(this Component self, ref T field, string objectName) where T : Component
    {
        if (field != null) return;
        var components = self.GetComponentsInChildren<T>(true);
        foreach (var comp in components)
        {
            if (comp.name.Equals(objectName, System.StringComparison.OrdinalIgnoreCase))
            {
                field = comp;
                return;
            }
        }
    }
}
