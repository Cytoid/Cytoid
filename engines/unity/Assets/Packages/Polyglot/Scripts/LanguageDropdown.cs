#if UNITY_5
using JetBrains.Annotations;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace Polyglot
{
#if UNITY_5_2 || UNITY_5_3 || UNITY_5_4_OR_NEWER
    [RequireComponent(typeof(Dropdown))]
#endif
    [AddComponentMenu("UI/Language Dropdown", 36)]
    public class LanguageDropdown : MonoBehaviour, ILocalize
    {
#if UNITY_5_2 || UNITY_5_3 || UNITY_5_4_OR_NEWER
        [Tooltip("The dropdown to populate with all the available languages")]

        [SerializeField]
        private Dropdown dropdown;

#if UNITY_5
        [UsedImplicitly]
#endif
        public void Reset()
        {
            dropdown = GetComponent<Dropdown>();
        }

#if UNITY_5
        [UsedImplicitly]
#endif
        public void Start()
        {
            CreateDropdown();

            Localization.Instance.AddOnLocalizeEvent(this);
        }

        private void CreateDropdown()
        {
            var flags = dropdown.hideFlags;
            dropdown.hideFlags = HideFlags.DontSaveInEditor;

            dropdown.options.Clear();

            var languageNames = Localization.Instance.EnglishLanguageNames;

            for (int index = 0; index < languageNames.Count; index++)
            {
                var languageName = languageNames[index];
                dropdown.options.Add(new Dropdown.OptionData(languageName));
            }

            dropdown.value = -1;
            dropdown.value = Localization.Instance.SelectedLanguageIndex;

            dropdown.hideFlags = flags;
        }

#endif
        public void OnLocalize()
        {
#if UNITY_5_2 || UNITY_5_3 || UNITY_5_4_OR_NEWER
            dropdown.onValueChanged.RemoveListener(Localization.Instance.SelectLanguage);
            dropdown.value = Localization.Instance.SelectedLanguageIndex;
            dropdown.onValueChanged.AddListener(Localization.Instance.SelectLanguage);
#endif
        }
    }
}
