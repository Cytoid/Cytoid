using System;
using UnityEngine;

namespace Polyglot
{
    [Serializable]
    public class LocalizationAsset
    {
        [SerializeField]
        private TextAsset textAsset;

        [SerializeField]
        private GoogleDriveDownloadFormat format = GoogleDriveDownloadFormat.CSV;

        public TextAsset TextAsset
        {
            get { return textAsset; }
            set { textAsset = value; }
        }

        public GoogleDriveDownloadFormat Format
        {
            get { return format; }
            set { format = value; }
        }
    }
}