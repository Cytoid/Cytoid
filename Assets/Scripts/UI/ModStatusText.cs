using UnityEngine;
using UnityEngine.UI;

namespace Cytoid.UI
{
	public class ModStatusText : MonoBehaviour
	{

		private Text text;

		private void Awake()
		{
			text = GetComponent<Text>();
		}

		private void Update()
		{
			text.text = PlayerPrefsExt.GetStringArray("mods", new string[0]).Length > 0 ? "On" : "Off";
		}
	}
}