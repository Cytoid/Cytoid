using UnityEngine;
using UnityEngine.UI;
using CW.Common;

namespace Lean.Common
{
	/// <summary>This component allows you to change the color of the Graphic (e.g. Image) attached to the current GameObject when selected.</summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(Graphic))]
	[HelpURL(LeanCommon.HelpUrlPrefix + "LeanSelectableGraphicColor")]
	[AddComponentMenu(LeanCommon.ComponentPathPrefix + "Selectable Graphic Color")]
	public class LeanSelectableGraphicColor : LeanSelectableBehaviour
	{
		/// <summary>The default color given to the SpriteRenderer.</summary>
		public Color DefaultColor { set { defaultColor = value; UpdateColor(); } get { return defaultColor; } } [SerializeField] private Color defaultColor = Color.white;

		/// <summary>The color given to the SpriteRenderer when selected.</summary>
		public Color SelectedColor { set { selectedColor = value; UpdateColor(); } get { return selectedColor; } } [SerializeField] private Color selectedColor = Color.green;

		[System.NonSerialized]
		private Graphic cachedGraphic;

		protected override void OnSelected(LeanSelect select)
		{
			UpdateColor();
		}

		protected override void OnDeselected(LeanSelect select)
		{
			UpdateColor();
		}

		public void UpdateColor()
		{
			if (cachedGraphic == null) cachedGraphic = GetComponent<Graphic>();

			var color = Selectable != null && Selectable.IsSelected == true ? selectedColor : defaultColor;

			cachedGraphic.color = color;
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Common.Editor
{
	using UnityEditor;
	using TARGET = LeanSelectableGraphicColor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanSelectableGraphicColor_Editor : CwEditor
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