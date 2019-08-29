using UnityEngine;

namespace Lean.Touch
{
	/// <summary>This component will automatically destroy this GameObject after the specified amount of time.
	/// NOTE: If you want to manually destroy this GameObject, then disable this component, and call the DestroyNow method directly.</summary>
	[HelpURL(LeanTouch.HelpUrlPrefix + "LeanDestroy")]
	public class LeanDestroy : MonoBehaviour
	{
		/// <summary>The amount of seconds remaining before this GameObject gets destroyed.</summary>
		[Tooltip("The amount of seconds remaining before this GameObject gets destroyed.")]
		public float Seconds = 1.0f;

		protected virtual void Update()
		{
			Seconds -= Time.deltaTime;

			if (Seconds <= 0.0f)
			{
				DestroyNow();
			}
		}

		/// <summary>You can manually call this method to destroy the current GameObject now.</summary>
		public void DestroyNow()
		{
			Destroy(gameObject);
		}
	}
}