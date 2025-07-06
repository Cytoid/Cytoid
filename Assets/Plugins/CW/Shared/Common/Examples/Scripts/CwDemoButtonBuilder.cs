using UnityEngine;
using UnityEngine.UI;
using CW.Common;

namespace CW.Common
{
	/// <summary>This component allows you to quickly build a UI button to activate only this GameObject when clicked.</summary>
	[HelpURL(CwShared.HelpUrlPrefix + "CwDemoButtonBuilder")]
	[AddComponentMenu(CwShared.ComponentMenuPrefix + "Demo Button Builder")]
	public class CwDemoButtonBuilder : MonoBehaviour
	{
		/// <summary>The built button will be based on this prefab.</summary>
		public GameObject ButtonPrefab { set { buttonPrefab = value; } get { return buttonPrefab; } } [SerializeField] private GameObject buttonPrefab;

		/// <summary>The built button will be placed under this transform.</summary>
		public RectTransform ButtonRoot { set { buttonRoot = value; } get { return buttonRoot; } } [SerializeField] private RectTransform buttonRoot;

		/// <summary>The icon given to this button.</summary>
		public Sprite Icon { set { icon = value; } get { return icon; } } [SerializeField] private Sprite icon;

		/// <summary>The icon will be tinted by this.</summary>
		public Color Color { set { color = value; } get { return color; } } [SerializeField] private Color color = Color.white;

		/// <summary>Use a different name for the button text?</summary>
		public string OverrideName { set { overrideName = value; } get { return overrideName; } } [SerializeField] [Multiline(3)] private string overrideName;

		[SerializeField]
		private GameObject clone;

		[ContextMenu("Build")]
		public void Build()
		{
			if (clone != null)
			{
				DestroyImmediate(clone);
			}

			if (buttonPrefab != null)
			{
				clone = DoInstantiate();

				clone.name = name;

				var image = clone.GetComponent<Image>();

				if (image != null)
				{
					image.sprite = icon;
					image.color  = color;
				}

				var title = clone.GetComponentInChildren<Text>();

				if (title != null)
				{
					title.text = string.IsNullOrEmpty(overrideName) == false ? overrideName : name;
				}

				var isolate = clone.GetComponent<CwDemoButton>();

				if (isolate != null)
				{
					isolate.IsolateTarget = transform;
				}
			}
		}

		[ContextMenu("Build All")]
		public void BuildAll()
		{
			foreach (var builder in transform.parent.GetComponentsInChildren<CwDemoButtonBuilder>(true))
			{
				builder.Build();
			}
		}

		private GameObject DoInstantiate()
		{
#if UNITY_EDITOR
			if (Application.isPlaying == false)
			{
				return (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(buttonPrefab, buttonRoot);
			}
#endif
			return Instantiate(buttonPrefab, buttonRoot, false);
		}
	}
}

#if UNITY_EDITOR
namespace PaintIn3D
{
	using UnityEditor;
	using TARGET = CwDemoButtonBuilder;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class CwDemoButtonBuilder_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("buttonPrefab", "The built button will be based on this prefab.");
			Draw("buttonRoot", "The built button will be placed under this transform.");

			Separator();

			Draw("icon", "The icon given to this button.");
			Draw("color", "The icon will be tinted by this.");
			Draw("overrideName", "Use a different name for the button text?");

			Separator();

			if (Button("Build All") == true)
			{
				Undo.RecordObjects(tgts, "Build All");

				tgt.BuildAll();
			}
		}
	}
}
#endif