using UnityEngine;

namespace CW.Common
{
	/// <summary>This component allows you to freely move the current GameObject based on mouse/finger drags.</summary>
	[HelpURL(CwShared.HelpUrlPrefix + "CwCameraMove")]
	[AddComponentMenu(CwShared.ComponentMenuPrefix + "Camera Move")]
	public class CwCameraMove : MonoBehaviour
	{
		/// <summary>Is this component currently listening for inputs?</summary>
		public bool Listen { set { listen = value; } get { return listen; } } [SerializeField] private bool listen = true;

		/// <summary>How quickly the position transitions from the current to the target value (-1 = instant).</summary>
		public float Damping { set { damping = value; } get { return damping; } } [SerializeField] private float damping = 10.0f;

		/// <summary>The movement speed will be multiplied by this.</summary>
		public float Sensitivity { set { sensitivity = value; } get { return sensitivity; } } [SerializeField] private float sensitivity = 1.0f;

		/// <summary>The keys/fingers required to move left/right.</summary>
		public CwInputManager.Axis HorizontalControls { set { horizontalControls = value; } get { return horizontalControls; } } [SerializeField] private CwInputManager.Axis horizontalControls = new CwInputManager.Axis(2, false, CwInputManager.AxisGesture.HorizontalDrag, 1.0f, KeyCode.A, KeyCode.D, KeyCode.LeftArrow, KeyCode.RightArrow, 100.0f);

		/// <summary>The keys/fingers required to move backward/forward.</summary>
		public CwInputManager.Axis DepthControls { set { depthControls = value; } get { return depthControls; } } [SerializeField] private CwInputManager.Axis depthControls = new CwInputManager.Axis(2, false, CwInputManager.AxisGesture.HorizontalDrag, 1.0f, KeyCode.S, KeyCode.W, KeyCode.DownArrow, KeyCode.UpArrow, 100.0f);

		/// <summary>The keys/fingers required to move down/up.</summary>
		public CwInputManager.Axis VerticalControls { set { verticalControls = value; } get { return verticalControls; } } [SerializeField] private CwInputManager.Axis verticalControls = new CwInputManager.Axis(3, false, CwInputManager.AxisGesture.HorizontalDrag, 1.0f, KeyCode.F, KeyCode.R, KeyCode.None, KeyCode.None, 100.0f);

		[System.NonSerialized]
		private Vector3 remainingDelta;

		protected virtual void Start()
		{
			CwInputManager.EnsureThisComponentExists();
		}

		protected virtual void Update()
		{
			if (listen == true)
			{
				AddToDelta();
			}

			DampenDelta();
		}

		private void AddToDelta()
		{
			// Get delta from binds
			var delta = default(Vector3);

			delta.x = horizontalControls.GetValue(Time.deltaTime);
			delta.y = verticalControls  .GetValue(Time.deltaTime);
			delta.z = depthControls     .GetValue(Time.deltaTime);

			// Store old position
			var oldPosition = transform.position;

			// Translate
			transform.Translate(delta * sensitivity, Space.Self);

			// Add to remaining
			var acceleration = transform.position - oldPosition;

			remainingDelta += acceleration;

			// Revert position
			transform.position = oldPosition;
		}

		private void DampenDelta()
		{
			// Dampen remaining delta
			var factor   = CwHelper.DampenFactor(damping, Time.deltaTime);
			var newDelta = Vector3.Lerp(remainingDelta, Vector3.zero, factor);

			// Translate by difference
			transform.position += remainingDelta - newDelta;

			// Update remaining
			remainingDelta = newDelta;
		}
	}
}

#if UNITY_EDITOR
namespace CW.Common
{
	using UnityEditor;
	using TARGET = CwCameraMove;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class CwCameraMove_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("listen", "Is this component currently listening for inputs?");
			Draw("damping", "How quickly the rotation transitions from the current to the target value (-1 = instant).");
			Draw("sensitivity", "The movement speed will be multiplied by this.");

			Separator();

			Draw("horizontalControls", "The keys/fingers required to move right/left.");
			Draw("depthControls", "The keys/fingers required to move backward/forward.");
			Draw("verticalControls", "The keys/fingers required to move down/up.");
		}
	}
}
#endif