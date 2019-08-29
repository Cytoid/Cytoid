using UnityEngine;

namespace Lean.Common.Examples
{
	/// <summary>This component allows you to open a URL using Unity events (e.g. a button).</summary>
	[AddComponentMenu("Lean/Common/Lean Open URL")]
	public class LeanOpenUrl : MonoBehaviour
	{
		public void Open(string url)
		{
			Application.OpenURL(url);
		}
	}
}