using UnityEngine;
using UnityEngine.UI;

namespace Lean.Touch
{
	/// <summary>This component allows you to change the color of the Graphic (e.g. Image) attached to the current GameObject when selected.</summary>
	[RequireComponent(typeof(Graphic))]
	[HelpURL(LeanTouch.HelpUrlPrefix + "LeanSelectableGraphicColor")]
	public class LeanSelectableGraphicColor : LeanSelectableBehaviour
	{
		[Tooltip("Automatically read the DefaultColor from the Renderer.material?")]
		public bool AutoGetDefaultColor;

		[Tooltip("The default color given to the Renderer.material")]
		public Color DefaultColor = Color.white;

		[Tooltip("The color given to the Renderer.material when selected")]
		public Color SelectedColor = Color.green;

#if UNITY_EDITOR
		protected virtual void Reset()
		{
			Start();
		}
#endif

		protected virtual void Start()
		{
			if (AutoGetDefaultColor == true)
			{
				var graphic = GetComponent<Graphic>();

				DefaultColor = graphic.color;
			}
		}

		protected override void OnSelect(LeanFinger finger)
		{
			ChangeColor(SelectedColor);
		}

		protected override void OnDeselect()
		{
			ChangeColor(DefaultColor);
		}

		private void ChangeColor(Color color)
		{
			var graphic = GetComponent<Graphic>();

			graphic.color = color;
		}
	}
}