using UnityEngine;

namespace CW.Common
{
	/// <summary>This makes the current <b>Transform</b> follow the <b>Target</b> Transform as if it were a child.</summary>
	[ExecuteInEditMode]
	[HelpURL(CwShared.HelpUrlPrefix + "CwFollow")]
	[AddComponentMenu(CwShared.ComponentMenuPrefix + "Follow")]
	public class CwFollow : MonoBehaviour
	{
		public enum FollowType
		{
			TargetTransform,
			MainCamera
		}

		public enum UpdateType
		{
			Update,
			LateUpdate
		}

		/// <summary>What should this component follow?</summary>
		public FollowType Follow { set { follow = value; } get { return follow; } } [SerializeField] private FollowType follow;

		/// <summary>The transform that will be followed.</summary>
		public Transform Target { set { target = value; } get { return target; } } [SerializeField] private Transform target;

		/// <summary>How quickly this Transform follows the target.
		/// -1 = instant.</summary>
		public float Damping { set { damping = value; } get { return damping; } } [SerializeField] private float damping = -1.0f;

		/// <summary>Follow the target's rotation too?</summary>
		public bool Rotate { set { rotate = value; } get { return rotate; } } [SerializeField] private bool rotate = true;

		/// <summary>Ignore Z axis for 2D?</summary>
		public bool IgnoreZ { set { ignoreZ = value; } get { return ignoreZ; } } [SerializeField] private bool ignoreZ;

		/// <summary>Where in the game loop should this component update?</summary>
		public UpdateType FollowIn { set { followIn = value; } get { return followIn; } } [SerializeField] private UpdateType followIn = UpdateType.LateUpdate;

		/// <summary>This allows you to specify a positional offset relative to the <b>Target</b>.</summary>
		public Vector3 LocalPosition { set { localPosition = value; } get { return localPosition; } } [SerializeField] private Vector3 localPosition;

		/// <summary>This allows you to specify a rotational offset relative to the <b>Target</b>.</summary>
		public Vector3 LocalRotation { set { localRotation = value; } get { return localRotation; } } [SerializeField] private Vector3 localRotation;

		/// <summary>This method will update the follow position now.</summary>
		[ContextMenu("UpdatePosition")]
		public void UpdatePosition()
		{
			var finalTarget = target;

			if (follow == FollowType.MainCamera)
			{
				var mainCamera = Camera.main;

				if (mainCamera != null)
				{
					finalTarget = mainCamera.transform;
				}
			}

			if (finalTarget != null)
			{
				var currentPosition = transform.position;
				var targetPosition  = finalTarget.TransformPoint(localPosition);
				var factor          = CwHelper.DampenFactor(damping, Time.deltaTime);

				if (ignoreZ == true)
				{
					targetPosition.z = currentPosition.z;
				}

				transform.position = Vector3.Lerp(currentPosition, targetPosition, factor);

				if (rotate == true)
				{
					var targetRotation = finalTarget.rotation * Quaternion.Euler(localRotation);

					transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, factor);
				}
			}
		}

		protected virtual void Update()
		{
			if (followIn == UpdateType.Update)
			{
				UpdatePosition();
			}
		}

		protected virtual void LateUpdate()
		{
			if (followIn == UpdateType.LateUpdate)
			{
				UpdatePosition();
			}
		}
	}
}

#if UNITY_EDITOR
namespace CW.Common
{
	using UnityEditor;
	using TARGET = CwFollow;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class CwFollow_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("follow", "What should this component follow?");
			if (Any(tgts, t => t.Follow == CwFollow.FollowType.TargetTransform))
			{
				BeginIndent();
					BeginError(Any(tgts, t => t.Target == null));
						Draw("target", "The transform that will be followed.");
					EndError();
				EndIndent();
			}
			Draw("damping", "How quickly this Transform follows the target.\n\n-1 = instant.");
			Draw("rotate", "Follow the target's rotation too?");
			Draw("ignoreZ", "Ignore Z axis for 2D?");
			Draw("followIn", "Where in the game loop should this component update?");

			Separator();

			Draw("localPosition", "This allows you to specify a positional offset relative to the Target transform.");
			Draw("localRotation", "This allows you to specify a rotational offset relative to the Target transform.");
		}
	}
}
#endif