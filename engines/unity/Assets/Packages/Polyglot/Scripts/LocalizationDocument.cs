using System;
using UnityEngine;

namespace Polyglot
{
    [Serializable]
    public class LocalizationDocument
    {
        [SerializeField]
        private string docsId;
        [SerializeField]
        private string sheetId;
        [SerializeField]
        private GoogleDriveDownloadFormat format;
        [SerializeField]
        private TextAsset textAsset;
        [SerializeField]
        private bool downloadOnStart;

        public TextAsset TextAsset
        {
            get { return textAsset; }
            set { textAsset = value; }
        }

        public string DocsId
        {
            get { return docsId; }
            set { docsId = value; }
        }

        public string SheetId
        {
            get { return sheetId; }
            set { sheetId = value; }
        }

        public GoogleDriveDownloadFormat Format
        {
            get { return format; }
            set { format = value; }
        }

        public bool DownloadOnStart
        {
            get { return downloadOnStart; }
            set { downloadOnStart = value; }
        }
    }
}