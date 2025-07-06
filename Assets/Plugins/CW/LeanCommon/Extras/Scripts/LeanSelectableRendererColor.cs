using UnityEngine;
using CW.Common;

namespace Lean.Common
{
	/// <summary>This component allows you to change the color of the Renderer (e.g. MeshRenderer) attached to the current GameObject when selected.</summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(Renderer))]
	[HelpURL(LeanCommon.HelpUrlPrefix + "LeanSelectableRendererColor")]
	[AddComponentMenu(LeanCommon.ComponentPathPrefix + "Selectable Renderer Color")]
	public class LeanSelectableRendererColor : LeanSelectableBehaviour
	{
		/// <summary>The default color given to the SpriteRenderer.</summary>
		public Color DefaultColor { set { defaultColor = value; UpdateColor(); } get { return defaultColor; } } [SerializeField] private Color defaultColor = Color.white;

		/// <summary>The color given to the SpriteRenderer when selected.</summary>
		public Color SelectedColor { set { selectedColor = value; UpdateColor(); } get { return selectedColor; } } [SerializeField] private Color selectedColor = Color.green;

		[System.NonSerialized]
		private Renderer cachedRenderer;

		[System.NonSerialized]
		private MaterialPropertyBlock properties;

		protected override void OnSelected(LeanSelect select)
		{
			UpdateColor();
		}

		protected override void OnDeselected(LeanSelect select)
		{
			UpdateColor();
		}

		protected override void Start()
		{
			base.Start();

			UpdateColor();
		}

		public void UpdateColor()
		{
			if (cachedRenderer == null) cachedRenderer = GetComponent<Renderer>();

			var color = Selectable != null && Selectable.IsSelected == true ? selectedColor : defaultColor;

			if (properties == null)
			{
				properties = new MaterialPropertyBlock();
			}

			cachedRenderer.GetPropertyBlock(properties);

			properties.SetColor("_Color", color);

			cachedRenderer.SetPropertyBlock(properties);
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Common.Editor
{
	using UnityEditor;
	using TARGET = LeanSelectableRendererColor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanSelectableRendererColor_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			var updateColor = false;

			Draw("defaultColor", ref updateColor, "The default color given to the SpriteRenderer.");
			Draw("selectedColor", ref updateColor, "The color given to the SpriteRenderer when selected.");

			if (updateColor == true)
			{
				Each(tgts, t => t.UpdateColor(), true);
			}
		}
	}
}
#endif