using UnityEngine;

namespace Lean.Touch
{
	/// <summary>This component allows you to open a URL using Unity events (e.g. a button).</summary>
	[HelpURL(LeanTouch.HelpUrlPrefix + "LeanOpenUrl")]
	public class LeanOpenUrl : MonoBehaviour
	{
		public Vector3 BaseScale = Vector3.one;

		public float Size = 1.0f;

		public float PulseInterval = 1.0f;

		public float PulseSize = 1.0f;

		public float Dampening = 5.0f;

		[System.NonSerialized]
		private float counter;

		public void Open(string url)
		{
			Application.OpenURL(url);
		}

		protected virtual void Update()
		{
			counter += Time.deltaTime;

			if (counter >= PulseInterval)
			{
				counter %= PulseInterval;

				Size += PulseSize;
			}

			var factor = LeanTouch.GetDampenFactor(Dampening, Time.deltaTime);

			Size = Mathf.Lerp(Size, 1.0f, factor);

			transform.localScale = Vector3.Lerp(transform.localScale, BaseScale * Size, factor);
		}
	}
}