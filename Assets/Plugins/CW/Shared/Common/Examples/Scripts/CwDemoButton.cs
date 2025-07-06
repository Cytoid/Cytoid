using UnityEngine;
using UnityEngine.EventSystems;

namespace CW.Common
{
	/// <summary>This component turns the current UI element into a button that links to the specified action.</summary>
	[ExecuteInEditMode]
	[HelpURL(CwShared.HelpUrlPrefix + "CwDemoButton")]
	[AddComponentMenu(CwShared.ComponentMenuPrefix + "Demo Button")]
	public class CwDemoButton : MonoBehaviour, IPointerDownHandler
	{
		public enum LinkType
		{
			PreviousScene,
			NextScene,
			Publisher,
			URL,
			Isolate
		}

		public enum ToggleType
		{
			KeepSelected,
			ToggleSelection,
			SelectPrevious
		}

		/// <summary>The action that will be performed when this UI element is clicked.</summary>
		public LinkType Link { set { link = value; } get { return link; } } [SerializeField] private LinkType link;

		/// <summary>The URL that will be opened.</summary>
		public string UrlTarget { set { urlTarget = value; } get { return urlTarget; } } [SerializeField] private string urlTarget;

		/// <summary>If this GameObject is active, then the button will be faded in.</summary>
		public Transform IsolateTarget { set { isolateTarget = value; } get { return isolateTarget; } } [SerializeField] private Transform isolateTarget;

		/// <summary>If this button is already selected and you click/tap it again, what should happen?</summary>
		public ToggleType IsolateToggle { set { isolateToggle = value; } get { return isolateToggle; } } [SerializeField] private ToggleType isolateToggle;

		[System.NonSerialized]
		private CanvasGroup cachedCanvasGroup;

		[System.NonSerialized]
		private Transform previousChild;

		protected virtual void OnEnable()
		{
			cachedCanvasGroup = GetComponent<CanvasGroup>();
		}

		protected virtual void Update()
		{
			var group = GetComponent<CanvasGroup>();

			if (group != null)
			{
				var alpha = 1.0f;

				switch (link)
				{
					case LinkType.PreviousScene:
					case LinkType.NextScene:
					{
						alpha = GetCurrentLevel() >= 0 && GetLevelCount() > 1 ? 1.0f : 0.0f;
					}
					break;
					case LinkType.Isolate:
					{
						if (isolateTarget != null)
						{
							alpha = isolateTarget.gameObject.activeInHierarchy == true ? 1.0f : 0.5f;
						}
					}
					break;
				}

				group.alpha          = alpha;
				group.blocksRaycasts = alpha > 0.0f;
				group.interactable   = alpha > 0.0f;
			}
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			switch (link)
			{
				case LinkType.PreviousScene:
				{
					var index = GetCurrentLevel();

					if (index >= 0)
					{
						if (--index < 0)
						{
							index = GetLevelCount() - 1;
						}

						LoadLevel(index);
					}
				}
				break;

				case LinkType.NextScene:
				{
					var index = GetCurrentLevel();

					if (index >= 0)
					{
						if (++index >= GetLevelCount())
						{
							index = 0;
						}

						LoadLevel(index);
					}
				}
				break;

				case LinkType.Publisher:
				{
					Application.OpenURL("https://carloswilkes.com");
				}
				break;

				case LinkType.URL:
				{
					if (string.IsNullOrEmpty(urlTarget) == false)
					{
						Application.OpenURL(urlTarget);
					}
				}
				break;

				case LinkType.Isolate:
				{
					if (isolateTarget != null)
					{
						var parent = isolateTarget.transform.parent;
						var active = isolateTarget.gameObject.activeSelf;

						foreach (Transform child in parent.transform)
						{
							if (child.gameObject.activeSelf == true)
							{
								if (child != isolateTarget)
								{
									previousChild = child;
								}

								child.gameObject.SetActive(false);
							}
						}

						switch (isolateToggle)
						{
							case ToggleType.KeepSelected:
							{
								isolateTarget.gameObject.SetActive(true);
							}
							break;

							case ToggleType.ToggleSelection:
							{
								isolateTarget.gameObject.SetActive(active == false);
							}
							break;

							case ToggleType.SelectPrevious:
							{
								if (active == true && previousChild != null)
								{
									previousChild.gameObject.SetActive(true);
								}
								else
								{
									isolateTarget.gameObject.SetActive(true);
								}
							}
							break;
						}
					}
				}
				break;
			}
		}

		private static int GetCurrentLevel()
		{
			var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
			var index = scene.buildIndex;

			if (index >= 0)
			{
				#if UNITY_EDITOR
					var scenes = UnityEditor.EditorBuildSettings.scenes;
					
					if (scenes.Length >= index || scenes[index].path != scene.path)
					{
						return -1;
					}
				#else
					if (UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(index).handle != scene.handle)
					{
						return -1;
					}
				#endif
			}

			return index;
		}

		private static int GetLevelCount()
		{
			#if UNITY_EDITOR
				return UnityEditor.EditorBuildSettings.scenes.Length;
			#else
				return UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
			#endif
		}

		private static void LoadLevel(int index)
		{
			UnityEngine.SceneManagement.SceneManager.LoadScene(index);
		}
	}
}

#if UNITY_EDITOR
namespace CW.Common
{
	using UnityEditor;
	using TARGET = CwDemoButton;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class CwDemoButton_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("link", "The action that will be performed when this UI element is clicked.");

			BeginIndent();
				if (Any(tgts, t => t.Link == CwDemoButton.LinkType.URL))
				{
					Draw("urlTarget", "The URL that will be opened.", "Target");
				}
				if (Any(tgts, t => t.Link == CwDemoButton.LinkType.Isolate))
				{
					Draw("isolateTarget", "If this GameObject is active, then the button will be faded in.", "Target");
					Draw("isolateToggle", "If this button is already selected and you click/tap it again, what should happen?", "Toggle");
				}
			EndIndent();
		}
	}
}
#endif