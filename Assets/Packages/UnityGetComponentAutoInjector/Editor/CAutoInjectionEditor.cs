#region Header
/* ============================================
 *	작성자 : KJH
   ============================================ */
#endregion Header

#if UNITY_EDITOR

namespace UnityEditor
{
	using UnityEngine;
	using UnityEditor.Callbacks;
	using System.Collections.Generic;

	[CustomEditor(typeof(MonoBehaviour), true), CanEditMultipleObjects]
	[InitializeOnLoad]
	public class CAutoInjectionEditor : Editor
	{
		private const string _isPressedPlayButton = "isPressedPlayButton";
		private const string _injectionToNextFrame = "injectionToNextFrame";
		private const string _isPlaying = "isPlaying";

		private enum PlayModeState
		{
			EnteredPlayMode, ExitingPlayMode
		}

		private static HashSet<int> _hashCodes = new HashSet<int>();

		static CAutoInjectionEditor()
		{
			EditorApplication.update += OnUpdateEditor;
		}

		[MenuItem("CONTEXT/MonoBehaviour/Force auto inject this")]
		private static void ForceInject(MenuCommand cmd)
		{
			InjectFrom_NoneSerializedObject(cmd.context, true);
		}

		[DidReloadScripts]
		private static void OnReloadScripts()
		{
			InjectFor_CurrentScene();
		}

		private static void OnPlayModeStateChanged(PlayModeState playModeState)
		{
			if (playModeState == PlayModeState.ExitingPlayMode)
				InjectFor_CurrentScene();
		}

		private static void OnUpdatePlayModeState()
		{
			if (EditorApplication.isPlaying)
			{
				if (EditorPrefs.GetBool(_isPlaying) == false)
				{
					EditorPrefs.SetBool(_isPlaying, true);
					OnPlayModeStateChanged(PlayModeState.EnteredPlayMode);
				}
			}
			else
			{
				if (EditorPrefs.GetBool(_isPlaying))
				{
					EditorPrefs.SetBool(_isPlaying, false);
					OnPlayModeStateChanged(PlayModeState.ExitingPlayMode);
				}
			}
		}

		private static void OnUpdateDetectCompiling()
		{
			if (EditorApplication.isPlaying && EditorApplication.isCompiling)
				EditorApplication.isPlaying = false;

			if (EditorPrefs.GetBool(_injectionToNextFrame))
			{
				InjectFor_CurrentScene();

				EditorPrefs.SetBool(_injectionToNextFrame, false);
				EditorApplication.isPlaying = true;
			}

			if (EditorApplication.isCompiling)
			{
				if (EditorApplication.isPlayingOrWillChangePlaymode && EditorPrefs.GetBool(_isPressedPlayButton) == false)
				{
					EditorPrefs.SetBool(_isPressedPlayButton, true);

					CDebug.Log("<color=red><b>Editor compiling and play mode is detected!\n",
					"After few seconds, the auto injection is completed and then play mode again.</b></color>");
				}

				EditorApplication.isPlaying = false;
			}
			else
			{
				if (EditorPrefs.GetBool(_isPressedPlayButton))
				{
					EditorPrefs.SetBool(_isPressedPlayButton, false);
					EditorPrefs.SetBool(_injectionToNextFrame, true);

					EditorApplication.isPlaying = false;
				}
			}
		}

		private static void OnUpdateEditor()
		{
			OnUpdatePlayModeState();
			OnUpdateDetectCompiling();
		}

		public static void InjectFor_CurrentScene(bool forceInject = false)
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode) return;

			List<GameObject> gameObjectList = GetSceneGameObjectsAll();

			int count = gameObjectList.Count;
			for (int i = 0; i < count; i++)
			{
				GameObject gameObject = gameObjectList[i];
				if (gameObject == null) continue;

				MonoBehaviour[] monos = gameObject.GetComponentsInChildren<MonoBehaviour>(true);

				int lenComponents = monos.Length;
				for (int j = 0; j < lenComponents; j++)
				{
					MonoBehaviour mono = monos[j];
					if (mono == null) continue;

					InjectFrom_NoneSerializedObject(mono, forceInject);
				}
			}
		}

		public static void InjectFrom_NoneSerializedObject(Object obj, bool forceInject)
		{
			InjectFrom_SerializedObject(new SerializedObject(obj), forceInject);
		}

		public static void InjectFrom_SerializedObject(SerializedObject serializedObject, bool forceInject = false)
		{
			if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;

			Object target = serializedObject.targetObject;
			if (target == null) return;

			if (PrefabUtility.GetPrefabType(target) == PrefabType.Prefab) return;

			int hashCode = target.GetHashCode();
			if (forceInject == false && _hashCodes.Contains(hashCode)) return;

			_hashCodes.Add(hashCode);

			serializedObject.Update();
				CAutoInjector.Inject(serializedObject, target, forceInject);
			serializedObject.ApplyModifiedProperties();
		}

		private void OnEnable()
		{
			InjectFrom_SerializedObject(serializedObject);
		}

		private static List<GameObject> GetSceneGameObjectsAll()
		{
			List<GameObject> newGameObjectList = new List<GameObject>();

			GameObject[] gameObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];

			int len = gameObjects.Length;
			for (int i = 0; i < len; i++)
			{
				GameObject gameObject = gameObjects[i];
				if (gameObject.hideFlags != HideFlags.None) continue;

				if (PrefabUtility.GetPrefabType(gameObject) == PrefabType.Prefab ||
					PrefabUtility.GetPrefabType(gameObject) == PrefabType.ModelPrefab) continue;

				newGameObjectList.Add(gameObject);
			}

			return newGameObjectList;
		}
	}
}

#endif