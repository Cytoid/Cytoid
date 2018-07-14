using System;
using UnityEngine;
using UnityEngine.UI;

namespace Cytoid.UI
{
    public class DataColorText : MonoBehaviour
    {
        public string Key;

        private InputField text;
        private string defaultValue;

        private void Awake()
        {
            text = GetComponent<InputField>();
            defaultValue = text.text;
            text.onEndEdit.AddListener(OnEndEdit);
            if (!PlayerPrefs.HasKey(Key))
            {
                PlayerPrefs.SetString(Key, defaultValue);
            }

            text.text = PlayerPrefs.GetString(Key);
        }

        private void OnEndEdit(string input)
        {
            try
            {
                Convert.HexToColor(input);
                PlayerPrefs.SetString(Key, input);
            }
            catch (Exception)
            {
                text.text = defaultValue;
                PlayerPrefs.SetString(Key, defaultValue);
            }
        }
    }
}