using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UploadRecord
{
    public int score;
    public double accuracy;
    public Details details;
    public List<string> mods;
    public bool ranked;
    public string hash;

    [Serializable]
    public class Details
    {
        public int perfect;
        public int great;
        public int good;
        public int bad;
        public int miss;
        public int maxCombo;

        public Info info;

        [Serializable]
        public class Info
        {
            public int clientVersion;
            public string uuid;
            public string os;
            public string model;
        }
        
        public Details FillDeviceInfo()
        {
            info = new Info
            {
                clientVersion = Context.VersionCode,
                uuid = SystemInfo.deviceUniqueIdentifier,
                os = SystemInfo.operatingSystem,
                model = SystemInfo.deviceModel
            };
            return this;
        }
    }
}