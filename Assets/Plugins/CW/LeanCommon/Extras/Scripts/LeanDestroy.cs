using UnityEngine;
using CW.Common;

namespace Lean.Common
{
	/// <summary>This component allows you to destroy a GameObject.</summary>
	[HelpURL(LeanCommon.HelpUrlPrefix + "LeanDestroy")]
	[AddComponentMenu(LeanCommon.ComponentPathPrefix + "Destroy")]
	public class LeanDestroy : MonoBehaviour
	{
		public enum ExecuteType
		{
			OnFirstFrame,
			AfterDelay,
			AfterDelayUnscaled,
			Manually
		}

		/// <summary>This allows you to control when the <b>Target</b> GameObject will be destroyed.
		/// OnFirstFrame = As soon as Update runs (this component must be enabled).
		/// AfterDelay = After the specified amount of <b>Seconds</b> has elapsed.
		/// AfterDelayUnscaled = The same as AfterDelay, but using unscaledDeltaTime.
		/// Manually = You must manually call the <b>DestroyNow</b> method.</summary>
		public ExecuteType Execute { set { execute = value; } get { return execute; } } [SerializeField] private ExecuteType execute = ExecuteType.Manually;

		/// <summary>The GameObject that will be destroyed.
		/// None/null = This GameObject.</summary>
		public GameObject Target { set { target = value; } get { return target; } } [SerializeField] private GameObject target;

		/// <summary>The amount of seconds remaining until the GameObject is destroyed.</summary>
		public float Seconds { set { seconds = value; } get { return seconds; } } [SerializeField] private float seconds = -1.0f;

		protected virtual void Update()
		{
			switch (execute)
			{
				case ExecuteType.OnFirstFrame:
				{
					DestroyNow();
				}
				break;

				case ExecuteType.AfterDelay:
				{
					seconds -= Time.deltaTime;

					if (seconds <= 0.0f)
					{
						DestroyNow();
					}
				}
				break;

				case ExecuteType.AfterDelayUnscaled:
				{
					seconds -= Time.unscaledDeltaTime;

					if (seconds <= 0.0f)
					{
						DestroyNow();
					}
				}
				break;
			}
		}

		/// <summary>You can manually call this method to destroy the specified GameObject immediately.</summary>
		public void DestroyNow()
		{
			execute = ExecuteType.Manually;

			Destroy(target != null ? target : gameObject);
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Common.Editor
{
	using UnityEditor;
	using TARGET = LeanDestroy;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanDestroy_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("target", "The GameObject that will be destroyed.\n\nNone/null = This GameObject.");
			Draw("execute", "This allows you to control when the <b>Target</b> GameObject will be destroyed.\n\nOnFirstFrame = As soon as Update runs (this component must be enabled).\n\nAfterDelay = After the specified amount of <b>Seconds</b> has elapsed.\n\nAfterDelayUnscaled = The same as AfterDelay, but using unscaledDeltaTime.\n\nManually = You must manually call the <b>DestroyNow</b> method.");
			if (Any(tgts, t => t.Execute == LeanDestroy.ExecuteType.AfterDelay || t.Execute == LeanDestroy.ExecuteType.AfterDelayUnscaled))
			{
				BeginIndent();
					Draw("seconds", "The amount of seconds remaining until the GameObject is destroyed.");
				EndIndent();
			}
		}
	}
}
#endif