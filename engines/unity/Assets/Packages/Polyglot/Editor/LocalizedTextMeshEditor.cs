#if UNITY_5
using JetBrains.Annotations;
#endif
using UnityEditor;

namespace Polyglot
{
#if UNITY_5
    [UsedImplicitly]
#endif
    [CustomEditor(typeof (LocalizedTextMesh))]
    [CanEditMultipleObjects]
    public class LocalizedTextMeshEditor : LocalizedEditor<LocalizedTextMesh>
    {
        public override void OnInspectorGUI()
        {
            OnInspectorGUI("key");
        }
    }
}