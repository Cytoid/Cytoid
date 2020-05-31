#if UNITY_5_3_OR_NEWER
using JetBrains.Annotations;
#endif
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Polyglot
{
    [CreateAssetMenu(fileName = "Localization.asset", menuName = "Polyglot Localization")]
    public class Localization : ScriptableObject
    {
        private const string KeyNotFound = "[{0}]";

        [SerializeField]
        private LocalizationDocument polyglotDocument = null;

        public LocalizationDocument PolyglotDocument { get { return polyglotDocument; } }
        

        [SerializeField]
        private LocalizationDocument customDocument = null;

        public LocalizationDocument CustomDocument { get { return customDocument; } }
        
        [Tooltip("The comma separated text files to get localization strings from\nThese are prioritized, so the ones added later are always prioritized.")]
        [SerializeField]
        private List<LocalizationAsset> inputFiles = null;

        public List<LocalizationAsset> InputFiles { get { return inputFiles; } }


        private static Localization instance;

        /// <summary>
        /// The singleton instance of this manager.
        /// </summary>
        public static Localization Instance
        {
            get
            {
                if (!HasInstance)
                {
                    Debug.LogError("Could not load Localization Settings from Resources");
                }
                return instance;
            }
            set { instance = value; }
        }

        private static bool HasInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<Localization>("Localization");
                }

                return instance != null;
            }
        }

        [Header("Language Support")]
        [Tooltip("The supported languages by the game.\n Leave empty if you support them all.")]
        [SerializeField]
        private List<Language> supportedLanguages = null;

        public List<Language> SupportedLanguages{ get { return supportedLanguages; }}

        [Tooltip("The currently selected language of the game.\nThis will also be the default when you start the game for the first time.")]
        [SerializeField]
        private Language selectedLanguage = Language.English;

        [Tooltip("If we cant find the string for the selected language we fall back to this language.")]
        [SerializeField]
        private Language fallbackLanguage = Language.English;
        
#region Arabic Support
#if ARABSUPPORT_ENABLED
        [Header("Arabic Support")]
    
        [Tooltip("Vowel marks in Arabic.")]
        [SerializeField]
        private bool showTashkeel = true;
    
        [SerializeField]
        private bool useHinduNumbers = false;
#endif
#endregion

        [Header("Event invoked when language is changed")]
        [Tooltip("This event is invoked every time the selected language is changed.")]
        public UnityEvent Localize = new UnityEvent();

        public LanguageDirection SelectedLanguageDirection
        {
            get { return GetLanguageDirection(SelectedLanguage); }
        }

        private LanguageDirection GetLanguageDirection(Language language)
        {
            switch (language)
            {
                case Language.Hebrew:
                    return LanguageDirection.RightToLeft;
                case Language.Arabic:
                    return LanguageDirection.RightToLeft;
                default:
                    return LanguageDirection.LeftToRight;
            }
        }

        public int SelectedLanguageIndex
        {
            get
            {
                if (supportedLanguages == null || supportedLanguages.Count == 0)
                {
                    return (int) SelectedLanguage;
                }

                return supportedLanguages.IndexOf(SelectedLanguage);
            }
        }

        public Language SelectedLanguage
        {
            get
            {
                return selectedLanguage;
            }
            set
            {
                if (IsLanguageSupported(value))
                {
                    if (value != selectedLanguage)
                    {
                        selectedLanguage = value;
                        InvokeOnLocalize();
                    }
                }
                else
                {
                    Debug.LogWarning(value + " is not a supported language.");
                }
            }
        }

        private bool IsLanguageSupported(Language language)
        {
            return supportedLanguages == null || supportedLanguages.Count == 0 || supportedLanguages.Contains(language);
        }

        public void InvokeOnLocalize()
        {
            if (Localize != null)
            {
                Localize.Invoke();
            }
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var localized = FindObjectsOfType<LocalizedText>();
                foreach (var local in localized)
                {
                    local.OnLocalize();
                }
            }
#endif
        }

        /// <summary>
        /// The english names of all available languages.
        /// </summary>
        public List<string> EnglishLanguageNames { get { return LocalizationImporter.GetLanguages("MENU_LANGUAGE_THIS_EN", supportedLanguages); } }

        /// <summary>
        /// The localized names of all available languages.
        /// </summary>
        public List<string> LocalizedLanguageNames { get { return LocalizationImporter.GetLanguages("MENU_LANGUAGE_THIS", supportedLanguages); } }

        /// <summary>
        /// The english name of the selected language.
        /// </summary>
        public string EnglishLanguageName { get { return Get("LANGUAGE_EN"); } }

        /// <summary>
        /// The Localized name of the selected language.
        /// </summary>
        public string LocalizedLanguageName { get { return Get("MENU_LANGUAGE_THIS"); } }

        /// <summary>
        /// Select a language, used by dropdowns and the like.
        /// </summary>
        /// <param name="selected"></param>
        public void SelectLanguage(int selected)
        {
            if (supportedLanguages == null || supportedLanguages.Count == 0)
            {
                SelectLanguage((Language) selected);
            }
            else
            {
                SelectedLanguage = supportedLanguages[selected];
            }
        }

        public void SelectLanguage(Language selected)
        {
            SelectedLanguage = selected;
        }

        public Language ConvertSystemLanguage(SystemLanguage selected)
        {
            switch (selected)
            {
                case SystemLanguage.Arabic:
                    return Language.Arabic;
                case SystemLanguage.Bulgarian:
                    return Language.Bulgarian;
                case SystemLanguage.Czech:
                    return Language.Czech;
                case SystemLanguage.Danish:
                    return Language.Danish;
                case SystemLanguage.Dutch:
                    return Language.Dutch;
                case SystemLanguage.English:
                    return Language.English;
                case SystemLanguage.Finnish:
                    return Language.Finnish;
                case SystemLanguage.French:
                    return Language.French;
                case SystemLanguage.German:
                    return Language.German;
                case SystemLanguage.Greek:
                    return Language.Greek;
                case SystemLanguage.Hebrew:
                    return Language.Hebrew;
                case SystemLanguage.Hungarian:
                    return Language.Hungarian;
                /*case SystemLanguage.Indonesian:
                    return Language.Indonesian;*/
                case SystemLanguage.Italian:
                    return Language.Italian;
                case SystemLanguage.Japanese:
                    return Language.Japanese;
                case SystemLanguage.Korean:
                    return Language.Korean;
                case SystemLanguage.Norwegian:
                    return Language.Norwegian;
                case SystemLanguage.Polish:
                    return Language.Polish;
                case SystemLanguage.Portuguese:
                    return Language.Portuguese;
                case SystemLanguage.Romanian:
                    return Language.Romanian;
                case SystemLanguage.Russian:
                    return Language.Russian;
                case SystemLanguage.Spanish:
                    return Language.Spanish;
                case SystemLanguage.Swedish:
                    return Language.Swedish;
                case SystemLanguage.Thai:
                    return Language.Thai;
                case SystemLanguage.Turkish:
                    return Language.Turkish;
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                    return Language.Simplified_Chinese;
                case SystemLanguage.ChineseTraditional:
                    return Language.Traditional_Chinese;
                case SystemLanguage.Vietnamese:
                    return Language.Vietnamese;
                case SystemLanguage.Indonesian:
                    return Language.Indonesian;
                case SystemLanguage.Ukrainian:
                    return Language.Ukrainian;
                /*case SystemLanguage.Afrikaans:
                    return Language.Afrikaans;
                    break;
                case SystemLanguage.Basque:
                    return Language.Basque;
                    break;
                case SystemLanguage.Belarusian:
                    return Language.Belarusian;
                    break;
                case SystemLanguage.Catalan:
                    return Language.Catalan;
                    break;
                case SystemLanguage.Chinese:
                    return Language.Chinese;
                    break;
                case SystemLanguage.Estonian:
                    return Language.Estonian;
                    break;
                case SystemLanguage.Faroese:
                    return Language.Faroese;
                    break;
                case SystemLanguage.Icelandic:
                    return Language.Icelandic;
                    break;
                case SystemLanguage.Latvian:
                    return Language.Latvian;
                    break;
                case SystemLanguage.Lithuanian:
                    return Language.Lithuanian;
                    break;
                case SystemLanguage.SerboCroatian:
                    return Language.SerboCroatian;
                    break;
                case SystemLanguage.Slovak:
                    return Language.Slovak;
                    break;
                case SystemLanguage.Slovenian:
                    return Language.Slovenian;
                    break;
                case SystemLanguage.Ukrainian:
                    return Language.Ukrainian;
                    break;
                case SystemLanguage.Vietnamese:
                    return Language.Vietnamese;
                    break;
                case SystemLanguage.Unknown:
                    break;*/
                default:
                    return selectedLanguage;
            }
        }

        /// <summary>
        /// Add a Localization listener to catch the event that is invoked when the selected language is changed.
        /// </summary>
        /// <param name="localize"></param>
        public void AddOnLocalizeEvent(ILocalize localize)
        {
            Localize.RemoveListener(localize.OnLocalize);
            Localize.AddListener(localize.OnLocalize);
            localize.OnLocalize();
        }
        /// <summary>
        /// Removes a Localization listener.
        /// </summary>
        /// <param name="localize"></param>
        public void RemoveOnLocalizeEvent(ILocalize localize)
        {
            Localize.RemoveListener(localize.OnLocalize);
        }

        /// <summary>
        /// Retrieves the correct language string by key.
        /// </summary>
        /// <param name="key">The key string</param>
        /// <returns>A localized string</returns>
        public static string Get(string key)
        {
            return Get(key, Instance.selectedLanguage);
        }

        public static string Get(string key, Language language)
        {
            var languages = LocalizationImporter.GetLanguages(key);
            var selected = (int) language;
            if (languages.Count > 0 && selected >= 0 && selected < languages.Count)
            {
                var currentString = languages[selected];
                if (string.IsNullOrEmpty(currentString) || LocalizationImporter.IsLineBreak(currentString))
                {
                    Debug.LogWarning("Could not find key " + key + " for current language " + language + ". Falling back to " + Instance.fallbackLanguage + " with " + languages[(int)Instance.fallbackLanguage]);
                    // EDIT: Cytoid
                    // Default Fujaoese to Simplified Chinese
                    selected = language == Language.Fujaoese ? (int) Language.Simplified_Chinese : (int) Instance.fallbackLanguage;
                    currentString = languages[selected];
                }

    #if ARABSUPPORT_ENABLED
                if (selected == (int) Language.Arabic)
                {
                    return ArabicSupport.ArabicFixer.Fix(currentString, instance.showTashkeel, instance.useHinduNumbers);
                }
    #endif
                
                return currentString;
            }

            return string.Format(KeyNotFound, key);
        }

        public static bool KeyExist(string key)
        {
            var languages = LocalizationImporter.GetLanguages(key);
            var selected = (int) Instance.selectedLanguage;
            return languages.Count > 0 && Instance.selectedLanguage >= 0 && selected < languages.Count;
        }

        public static List<string> GetKeys()
        {
            return LocalizationImporter.GetKeys();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public static string GetFormat(string key, params object[] arguments)
        {
            if (string.IsNullOrEmpty(key) || arguments == null || arguments.Length == 0)
            {
                return Get(key);
            }

            return string.Format(Get(key), arguments);
        }

        public bool InputFilesContains(LocalizationDocument doc)
        {
            foreach (var inputFile in inputFiles)
            {
                if (inputFile != null && inputFile.TextAsset == doc.TextAsset && inputFile.Format == doc.Format)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
