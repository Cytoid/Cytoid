using UnityEngine;
using CW.Common;

namespace Lean.Common
{
	/// <summary>This component rotates the current GameObject based on the current Angle value.
	/// NOTE: This component overrides and takes over the rotation of this GameObject, so you can no longer externally influence it.</summary>
	[ExecuteInEditMode]
	[HelpURL(LeanCommon.HelpUrlPrefix + "LeanRoll")]
	[AddComponentMenu(LeanCommon.ComponentPathPrefix + "Roll")]
	public class LeanRoll : MonoBehaviour
	{
		/// <summary>The current angle in degrees.</summary>
		public float Angle { set { angle = value; } get { return angle; } } [SerializeField] private float angle;

		/// <summary>Should the <b>Angle</b> value be clamped?</summary>
		public bool Clamp { set { clamp = value; } get { return clamp; } } [SerializeField] private bool clamp;

		/// <summary>The minimum <b>Angle</b> value.</summary>
		public float ClampMin { set { clampMin = value; } get { return clampMin; } } [SerializeField] private float clampMin;

		/// <summary>The maximum <b>Angle</b> value.</summary>
		public float ClampMax { set { clampMax = value; } get { return clampMax; } } [SerializeField] private float clampMax;

		/// <summary>If you want this component to change smoothly over time, then this allows you to control how quick the changes reach their target value.
		/// -1 = Instantly change.
		/// 1 = Slowly change.
		/// 10 = Quickly change.</summary>
		public float Damping { set { damping = value; } get { return damping; } } [SerializeField] private float damping = -1.0f;

		[SerializeField]
		private float currentAngle;

		/// <summary>The <b>Angle</b> value will be incremented by the specified angle in degrees.</summary>
		public void IncrementAngle(float delta)
		{
			angle += delta;
		}

		/// <summary>The <b>Angle</b> value will be decremented by the specified angle in degrees.</summary>
		public void DecrementAngle(float delta)
		{
			angle -= delta;
		}

		/// <summary>This method will update the Angle value based on the specified vector.</summary>
		public void RotateToDelta(Vector2 delta)
		{
			if (delta.sqrMagnitude > 0.0f)
			{
				angle = Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg;
			}
		}

		/// <summary>This method will immediately snap the current angle to its target value.</summary>
		[ContextMenu("Snap To Target")]
		public void SnapToTarget()
		{
			currentAngle = angle;
		}

		protected virtual void Start()
		{
			currentAngle = angle;
		}

		protected virtual void Update()
		{
			// Get t value
			var factor = CwHelper.DampenFactor(damping, Time.deltaTime);

			if (clamp == true)
			{
				angle = Mathf.Clamp(angle, clampMin, clampMax);
			}

			// Lerp angle
			currentAngle = Mathf.LerpAngle(currentAngle, angle, factor);

			// Update rotation
			transform.rotation = Quaternion.Euler(0.0f, 0.0f, -currentAngle);
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Common.Editor
{
	using UnityEditor;
	using TARGET = LeanRoll;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanRoll_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("angle", "The current angle in degrees.");
			Draw("clamp", "Should the Angle value be clamped?");

			if (Any(tgts, t => t.Clamp == true))
			{
				BeginIndent();
					Draw("clampMin", "The minimum Angle value.", "Min");
					Draw("clampMax", "The maximum Angle value.", "Max");
				EndIndent();

				Separator();
			}

			Draw("damping", "If you want this component to change smoothly over time, then this allows you to control how quick the changes reach their target value.\n\n-1 = Instantly change.\n\n1 = Slowly change.\n\n10 = Quickly change.");
		}
	}
}
#endif