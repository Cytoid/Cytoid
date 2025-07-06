using UnityEngine;
using CW.Common;

namespace Lean.Touch
{
	/// <summary>This component will pulse the transform.localScale value over time.</summary>
	[HelpURL(LeanTouch.HelpUrlPrefix + "LeanPulseScale")]
	[AddComponentMenu(LeanTouch.ComponentPathPrefix + "Pulse Scale")]
	public class LeanPulseScale : MonoBehaviour
	{
		/// <summary>The default scale.</summary>
		public Vector3 BaseScale { set { baseScale = value; } get { return baseScale; } } [SerializeField] private Vector3 baseScale = Vector3.one;

		/// <summary>The current scale multiplier.</summary>
		public float Size { set { size = value; } get { return size; } } [SerializeField] private float size = 1.0f;

		/// <summary>The interval between each pulse in seconds.</summary>
		public float PulseInterval { set { pulseInterval = value; } get { return pulseInterval; } } [SerializeField] private float pulseInterval = 1.0f;

		/// <summary>The amount Size will be incremented each pulse.</summary>
		public float PulseSize { set { pulseSize = value; } get { return pulseSize; } } [SerializeField] private float pulseSize = 1.0f;

		/// <summary>If you want this component to change smoothly over time, then this allows you to control how quick the changes reach their target value.
		/// -1 = Instantly change.
		/// 1 = Slowly change.
		/// 10 = Quickly change.</summary>
		public float Damping { set { damping = value; } get { return damping; } } [SerializeField] private float damping = 5.0f;

		[System.NonSerialized]
		private float counter;

		protected virtual void Update()
		{
			counter += Time.deltaTime;

			if (counter >= pulseInterval)
			{
				counter %= pulseInterval;

				size += pulseSize;
			}

			var factor = CwHelper.DampenFactor(damping, Time.deltaTime);

			size = Mathf.Lerp(size, 1.0f, factor);

			transform.localScale = Vector3.Lerp(transform.localScale, baseScale * size, factor);
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Touch.Editor
{
	using UnityEditor;
	using TARGET = LeanPulseScale;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET), true)]
	public class LeanPulseScale_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("baseScale", "The default scale.");
			Draw("size", "The current scale multiplier.");
			Draw("pulseInterval", "The interval between each pulse in seconds.");
			Draw("pulseSize", "The amount Size will be incremented each pulse.");
			Draw("damping", "If you want this component to change smoothly over time, then this allows you to control how quick the changes reach their target value.\n\n-1 = Instantly change.\n\n1 = Slowly change.\n\n10 = Quickly change.");
		}
	}
}
#endif