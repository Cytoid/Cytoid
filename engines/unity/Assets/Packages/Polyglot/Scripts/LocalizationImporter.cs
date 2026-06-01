using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Polyglot
{
    public static class LocalizationImporter
    {
        /// <summary>
        /// A dictionary with the key of the text item you want and a list of all the languages.
        /// </summary>
        private static Dictionary<string, List<string>> languageStrings = new Dictionary<string, List<string>>();

        private static List<string> EmptyList = new List<string>();

        private static List<LocalizationAsset> InputFiles = new List<LocalizationAsset>();

        static LocalizationImporter()
        {
            Initialize();
        }

        private static void Initialize()
        {
            var settings = Localization.Instance;
            if (settings == null)
            {
                Debug.LogError("Could not find a Localization Settings file in Resources.");
                return;
            }

            languageStrings.Clear();
            ImportFromFiles(settings);
            ImportFromGoogle(settings);
        }

        private static void ImportFromGoogle(Localization settings)
        {
            if (settings.CustomDocument.DownloadOnStart)
            {
                Download(settings.PolyglotDocument, s => Import(s, settings.PolyglotDocument.Format));
            }

            if (settings.CustomDocument.DownloadOnStart)
            {
                Download(settings.CustomDocument, s => Import(s, settings.CustomDocument.Format));
            }
        }

        private static void Import(string text, GoogleDriveDownloadFormat format)
        {
            ImportTextFile(text, format);
        }

        private static IEnumerator Download(LocalizationDocument document, Action<string> done, Func<float, bool> progressbar = null)
        {
            return GoogleDownload.DownloadSheet(document.DocsId, document.SheetId, done, document.Format, progressbar);
        }
        
        public static IEnumerator DownloadPolyglotSheet( Func<float, bool> progressbar = null)
        {
            var settings = Localization.Instance;
            if (settings == null)
            {
                Debug.LogError("Could not find a Localization Settings file in Resources.");
                return null;
            } 
            return Download(settings.PolyglotDocument, s => Import(s, settings.PolyglotDocument.Format), progressbar);
        }

        public static IEnumerator DownloadCustomSheet(Func<float, bool> progressbar = null)
        {
            var settings = Localization.Instance;
            if (settings == null)
            {
                Debug.LogError("Could not find a Localization Settings file in Resources.");
                return null;
            } 
            return Download(settings.CustomDocument, s => Import(s, settings.CustomDocument.Format), progressbar);
        }

        private static void ImportFromFiles(Localization settings)
        {
            InputFiles.Clear();
            InputFiles.AddRange(settings.InputFiles);
            ImportInputFiles();
        }

        private static void ImportInputFiles()
        {
            for (var index = 0; index < InputFiles.Count; index++)
            {
                var inputAsset = InputFiles[index];

                if (inputAsset == null)
                {
                    Debug.LogError("Input File at index [" + index + "] is null");
                    continue;
                }
                
                if (inputAsset.TextAsset == null)
                {
                    Debug.LogError("Input File Text Asset at index [" + index + "] is null");
                    continue;
                }

                ImportTextFile(inputAsset.TextAsset.text, inputAsset.Format);
            }
        }

        private static void ImportTextFile(string text, GoogleDriveDownloadFormat format)
        {
            List<List<string>> rows;
            text = text.Replace("\r\n", "\n");
            if (format == GoogleDriveDownloadFormat.CSV)
            {
                rows = CsvReader.Parse(text);
            }
            else
            {
                rows = TsvReader.Parse(text);
            }
            var canBegin = false;
            
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var key = row[0];
                
                if (string.IsNullOrEmpty(key) || IsLineBreak(key) || row.Count <= 1)
                {
                    //Ignore empty lines in the sheet
                    continue;
                }
                
                if (!canBegin)
                {
                    if (key == "Polyglot" || key == "PolyMaster" || key == "BEGIN")
                    {
                        canBegin = true;
                    }
                    continue;
                }

                if (key == "END")
                {
                    break;
                }

                //Remove key
                row.RemoveAt(0);
                //Remove description
                row.RemoveAt(0);

                if (languageStrings.ContainsKey(key))
                {
                    Debug.Log("The key '" + key + "' already exist, but is now overwritten");
                    languageStrings[key] = row;
                    continue;
                }
                languageStrings.Add(key, row);
            }
        }

        /// <summary>
        /// Checks if the current string is \r or \n
        /// </summary>
        /// <param name="currentString"></param>
        /// <returns></returns>
        public static bool IsLineBreak(string currentString)
        {
            return currentString.Length == 1 && (currentString[0] == '\r' || currentString[0] == '\n')
                || currentString.Length == 2 && currentString.Equals(Environment.NewLine);
        }

        /// <summary>
        /// Get all language strings on a given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static List<string> GetLanguages(string key, List<Language> supportedLanguages = null)
        {
            if (languageStrings == null || languageStrings.Count == 0)
            {
                ImportInputFiles();
            }

            if (string.IsNullOrEmpty(key) || !languageStrings.ContainsKey(key))
            {
                return EmptyList;
            }

            if (supportedLanguages == null || supportedLanguages.Count == 0)
            {
                return languageStrings[key];
            }

            // Filter the supported languages down to the supported ones
            var supportedLanguageStrings = new List<string>(supportedLanguages.Count);
            for (int index = 0; index < supportedLanguages.Count; index++)
            {
                var language = supportedLanguages[index];
                var supportedLanguage = (int) language;
                supportedLanguageStrings.Add(languageStrings[key][supportedLanguage]);
            }
            return supportedLanguageStrings;
        }

        public static Dictionary<string, List<string>> GetLanguagesStartsWith(string key)
        {
            if (languageStrings == null || languageStrings.Count == 0)
            {
                ImportInputFiles();
            }

            var multipleLanguageStrings = new Dictionary<string, List<string>>();
            foreach (var languageString in languageStrings)
            {
                if (languageString.Key.ToLower().StartsWith(key.ToLower()))
                {
                    multipleLanguageStrings.Add(languageString.Key, languageString.Value);
                }
            }

            return multipleLanguageStrings;
        }

        public static Dictionary<string, List<string>> GetLanguagesContains(string key)
        {
            if (languageStrings == null || languageStrings.Count == 0)
            {
                ImportInputFiles();
            }

            var multipleLanguageStrings = new Dictionary<string, List<string>>();
            foreach (var languageString in languageStrings)
            {
                if (languageString.Key.ToLower().Contains(key.ToLower()))
                {
                    multipleLanguageStrings.Add(languageString.Key, languageString.Value);
                }
            }

            return multipleLanguageStrings;
        }

        public static void Refresh()
        {
            Initialize();
            if (Localization.Instance != null)
            {
                Localization.Instance.InvokeOnLocalize();
            }
        }

        public static List<string> GetKeys()
        {
            return languageStrings.Keys.ToList();
        }
    }
}