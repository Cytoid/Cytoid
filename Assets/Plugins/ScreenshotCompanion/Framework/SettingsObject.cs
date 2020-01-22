using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ScreenshotCompanionFramework
{
    [Serializable]
    public class SettingsaObject
    {
		// tool appearance
        public Color signatureColor = new Color(1f, 0f, 0.5f);



		// file
		public string customFileName = "";
        public bool includeProject = true;
		public bool includeCamera = true;
        public bool includeDate = true;
        public bool includeResolution = false;
        public bool includeCounter = false;
        public int counter = 0;
        public ScreenshotCompanion.FileType fileType;



		// path
		public bool applicationPath = false;
        public string customDirectoryName = "";
        

		
		// capturing
		public ScreenshotCompanion.CaptureMethod captureMethod;
        public bool singleCamera = false;
        public float renderSizeMultiplier = 1f;
        public int captureSizeMultiplier = 1;
        public Vector2 cutoutPosition;
        public Vector2 cutoutSize; 
    }
}