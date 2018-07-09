using UnityEngine;
using UnityEngine.UI;

namespace Cytoid.UI
{
    public class DataToggle : MonoBehaviour
    {
        public string Key;

        private Toggle toggle;
        
        private void Awake()
        {
            toggle = GetComponentInChildren<Toggle>();
            toggle.onValueChanged.AddListener(OnValueChanged);
            if (!PlayerPrefs.HasKey(Key))
            {
                PlayerPrefsExt.SetBool(Key, toggle.isOn);
            }
            toggle.isOn = PlayerPrefsExt.GetBool(Key);
        }

        private void OnValueChanged(bool on)
        {
            PlayerPrefsExt.SetBool(Key, on);
        }
    }
}