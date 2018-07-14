using UnityEngine;
using UnityEngine.UI;

namespace Cytoid.UI
{
    public class DataSlider : MonoBehaviour
    {
        public string Key;

        private Slider slider;

        private void Awake()
        {
            slider = GetComponentInChildren<Slider>();
            slider.onValueChanged.AddListener(OnValueChanged);
            if (!PlayerPrefs.HasKey(Key))
            {
                PlayerPrefs.SetFloat(Key, slider.value);
            }

            slider.value = PlayerPrefs.GetFloat(Key);
        }

        private void OnValueChanged(float value)
        {
            PlayerPrefs.SetFloat(Key, slider.value);
        }
    }
}