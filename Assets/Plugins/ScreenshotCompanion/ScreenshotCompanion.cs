using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using ScreenshotCompanionFramework;

// ScreenshotCompanion
[ExecuteInEditMode]
public class ScreenshotCompanion : MonoBehaviour
{

    // ENUMS
    public enum FileType { PNG, JPG };
    public enum CaptureMethod { CaptureScreenshot, RenderTexture, Cutout };



    public List<CameraObject> list = new List<CameraObject>();
    public SettingsaObject settings;



    [HideInInspector] public bool foldoutSettings = false;
    [HideInInspector] public int lastCamID = 0;
    [HideInInspector] public Camera lastCam;


    GUIStyle cutoutBoxStyle = null;



    public void Create()
    {
        list.Add(new CameraObject());
    }

    public void RequestDelete(int id)
    {
        list[id].deleteQuestion = true;
    }

    public void Delete(int id)
    {
        list.Remove(list[id]);

        if (list.Count == 0)
        {
            Create();
        }
    }

    void Awake()
    {
        if (list.Count == 0)
        {
            Create();
            list[0].cam = Camera.main.gameObject;
        }
    }

    void LateUpdate()
    {
        if (Input.anyKeyDown)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if ((int)list[i].hotkey == 0)
                {
                    continue;
                }

                if (Input.GetKeyDown(list[i].hotkey.ToString().ToLower()))
                {
                    if (list[i] != null)
                    {
                        if (settings.captureMethod == CaptureMethod.RenderTexture)
                        {
                            Camera attachedCam = list[i].cam.GetComponent<Camera>();
                            if (attachedCam == null)
                            {
                                CaptureScreenshots(i, true);
                            }
                            else
                            {
                                CaptureRenderTexture(attachedCam, i);
                            }
                        }
                        else if (settings.captureMethod == CaptureMethod.CaptureScreenshot)
                        {
                            CaptureScreenshots(i, false);
                        }
                        else
                        {
                            StartCoroutine(CaptureCutout(i));
                        }

                        lastCam = list[lastCamID].cam.GetComponent<Camera>();
                    }
                    else
                    {
                        DebugLogExtended("Screenshot by Hotkey (" + list[i].hotkey + ") could not be created! Camera not available.");
                    }
                }
            }
        }
    }

    void activateCameraID(int id)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].cam != null)
                list[i].cam.SetActive(false);
        }
        list[id].cam.SetActive(true);
    }

    public string getSaveDirectory()
    {
        string pickDirectory = settings.customDirectoryName != "" ? settings.customDirectoryName : "Screenshots";

        if (settings.applicationPath)
        { // path to a safe location depending on the platform
            return Application.persistentDataPath + "/" + pickDirectory + "/";
        }
        else
        { // path to Unity project main folder
            return Directory.GetCurrentDirectory() + "/" + pickDirectory + "/";
        }
    }

    string checkSaveDirectory()
    {
        string directoryPath = getSaveDirectory();

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        return directoryPath;
    }

    void initCutoutBoxStyle()
    {
        //if (cutoutBoxStyle == null) {
        cutoutBoxStyle = new GUIStyle(GUI.skin.box);

        int d = 16;

        Color[] c = new Color[d * d];
        for (int x = 0; x < d; x++)
        {
            for (int y = 0; y < d; y++)
            {
                if (x == 0 || x == d - 1 || y == 0 || y == d - 1)
                {
                    c[x * d + y] = Color.white;
                }
                else
                {
                    c[x * d + y] = new Color(1f, 1f, 1f, 0.1f);
                }
            }
        }

        Texture2D t = new Texture2D(d, d);
        t.SetPixels(c);
        t.Apply();

        cutoutBoxStyle.normal.background = t;
        //}
    }

    void clampCutoutBox()
    {
        settings.cutoutPosition.x = Mathf.Clamp(settings.cutoutPosition.x, settings.cutoutSize.x / 2f, (float)Screen.width - settings.cutoutSize.x / 2f);
        settings.cutoutPosition.y = Mathf.Clamp(settings.cutoutPosition.y, settings.cutoutSize.y / 2f, (float)Screen.height - settings.cutoutSize.y / 2f);

        settings.cutoutSize.x = Mathf.Clamp(settings.cutoutSize.x, 0f, (float)Screen.width);
        settings.cutoutSize.y = Mathf.Clamp(settings.cutoutSize.y, 0f, (float)Screen.height);
    }

    Vector2 lastP;
    Vector2 lastS;
    float timer = 0f;

    void OnGUI()
    {
        if (settings == null)
        {
            return;
        }

        if (lastP.x == settings.cutoutPosition.x && lastP.y == settings.cutoutPosition.y && lastS.x == settings.cutoutSize.x && lastS.y == settings.cutoutSize.y)
        {
            timer -= Time.deltaTime;
        }
        else
        {
            timer = 1f;
        }

        lastP = settings.cutoutPosition;
        lastS = settings.cutoutSize;

        if (timer <= 0f)
        {
            return;
        }

        if (settings.captureMethod == CaptureMethod.Cutout)
        {
            initCutoutBoxStyle();

            clampCutoutBox();

            GUI.Box(new Rect(settings.cutoutPosition.x - settings.cutoutSize.x / 2f, settings.cutoutPosition.y - settings.cutoutSize.y / 2f, settings.cutoutSize.x, settings.cutoutSize.y), "", cutoutBoxStyle);
        }
    }

    public void CaptureCutoutVoid(int id)
    {
        if (settings.singleCamera)
        {
            activateCameraID(id);
        }

        StartCoroutine(CaptureCutout(id));
    }

    // create a more blurry screenshot if there are multiple cameras or no camera is found on the GameObject
    public IEnumerator CaptureCutout(int id)
    {
        yield return new WaitForEndOfFrame();

        /*
		if (singleCamera) {
			activateCameraID(id);
		}
		*/

        string directoryName = checkSaveDirectory();
        string fileName = directoryName + getFileName(id);

        cutoutEmptyCheck();
        clampCutoutBox();

        var startX = (int)(settings.cutoutPosition.x - settings.cutoutSize.x / 2f);
        var startY = (int)((Screen.height - settings.cutoutPosition.y) - settings.cutoutSize.y / 2f);
        var width = (int)settings.cutoutSize.x;
        var height = (int)settings.cutoutSize.y;
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);

        tex.ReadPixels(new Rect(startX, startY, width, height), 0, 0);
        tex.Apply();

        var bytes = tex.EncodeToPNG();
        Destroy(tex);

        File.WriteAllBytes(fileName, bytes);

        DebugLogExtended("Captured newCutout Screenshot (" + width + "x" + height + " at " + startX + "," + startY + ") saved to: " + fileName);

        postCapture();
    }

    void cutoutEmptyCheck()
    {
        if (settings.cutoutSize.x <= 8f || settings.cutoutSize.y <= 8f)
        {
            Debug.Log("[ScreenshotCompanion] A size of less than 8x8 pixels for Cutout has been detected!");
            if (Screen.width < 500 || Screen.height < 500)
            {
                Debug.Log("[ScreenshotCompanion] Reset to 500x500 pixels!");
                settings.cutoutSize = new Vector2(500f, 500f);
            }
            else
            {
                Debug.Log("[ScreenshotCompanion] Reset to " + Screen.width + "x" + Screen.height + " pixels!");
                settings.cutoutSize = new Vector2((float)Screen.width, (float)Screen.height);
            }
        }
    }

    // create a more blurry screenshot if there are multiple cameras or no camera is found on the GameObject
    public void CaptureScreenshots(int id, bool fallback)
    {
        if (settings.singleCamera)
        {
            activateCameraID(id);
        }

        string directoryName = checkSaveDirectory();
        string fileName = directoryName + getFileName(id);

        ScreenCapture.CaptureScreenshot(fileName, settings.captureSizeMultiplier);

        if (fallback)
        {
            Debug.Log("Fallback to Application.CaptureScreenshot because a GameObject without Camera (or Camera group) was used. Screenshot saved to: " + fileName);
        }
        else
        {
            Debug.Log("Screenshot saved to: " + fileName);
        }

        postCapture();
    }

    // create a sharp screenshot for a single Camera
    public void CaptureRenderTexture(Camera attachedCam, int id)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].cam != null)
                list[i].cam.SetActive(false);
        }
        list[id].cam.SetActive(true);

        string directoryName = checkSaveDirectory();
        string fileName = directoryName + getFileName(id);

        int resWidth = (int)(attachedCam.pixelWidth * settings.renderSizeMultiplier);
        int resHeight = (int)(attachedCam.pixelHeight * settings.renderSizeMultiplier);

        RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);

        attachedCam.targetTexture = rt;
        Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
        attachedCam.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        attachedCam.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);
        byte[] bytes = screenShot.EncodeToPNG();

        System.IO.File.WriteAllBytes(fileName, bytes);
        Debug.Log("Screenshot saved to: " + fileName);

        postCapture();
    }

    public string getFileName(int camID)
    {
        string fileName = "";

        bool gotHeader = false;

        // custom name
        if (settings.customFileName != "")
        {
            fileName += settings.customFileName;
            gotHeader = true;
        } 
        // add project name
        else if (settings.includeProject)
        { 
            fileName += Application.productName;
            gotHeader = true;
        }

        /* 
        if (settings.customFileName != "")
        {
            fileName += settings.customFileName;
        }
        else
        {
            string dp = Application.dataPath;
            string[] s;
            s = dp.Split("/"[0]);
            fileName += s[s.Length - 2];
        }
        */

        // add camera name
        if (settings.includeCamera)
        {
            if (gotHeader){
                fileName += "_";
            }

            if (camID < 0 || camID >= list.Count || list[camID] == null || list[camID].cam == null)
            {
                fileName += "CameraName";
                lastCamID = 0;
            }
            else
            {
                fileName += list[camID].cam.name;
                lastCamID = camID;
            }

            gotHeader = true;
        }

        // add date
        if (settings.includeDate)
        {
            if (gotHeader){
                fileName += "_";
            }

            fileName += DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            gotHeader = true;
        }

        // add resolution
        if (settings.includeResolution)
        {
            if (gotHeader){
                fileName += "_";
            }

            fileName += getResolution();
            gotHeader = true;
        }

        // add project name
        if (settings.includeCounter)
        {
            if (gotHeader){
                fileName += "_";
            }

            fileName += settings.counter.ToString("D4");
            gotHeader = true;
        }

        // if the filename is empty, add the date at least
        if (fileName == ""){
            fileName += DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            
        }

        // select filetype
        if (settings.fileType == FileType.JPG)
        {
            fileName += ".jpg";
        }
        else if (settings.fileType == FileType.PNG)
        {
            fileName += ".png";
        }

        return fileName;
    }

    public string getResolution()
    {
        //return gameViewDimensions.width * superSize + "x" + gameViewDimensions.height * superSize;

        if (lastCam == null || list[lastCamID].cam != lastCam.gameObject)
        {
            if (list[lastCamID].cam != null)
            {
                lastCam = list[lastCamID].cam.GetComponentInChildren<Camera>();
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] == null || list[i].cam == null)
                        continue;
                    lastCam = list[i].cam.GetComponentInChildren<Camera>();
                    if (lastCam != null)
                    {
                        break;
                    }
                }
            }
        }

        if (lastCam == null)
        {
            return "-x-";
        }

        if (settings.captureMethod == CaptureMethod.RenderTexture)
        {
            return (int)(lastCam.pixelWidth * settings.renderSizeMultiplier) + "x" + (int)(lastCam.pixelHeight * settings.renderSizeMultiplier);
        }

        return lastCam.pixelWidth * settings.captureSizeMultiplier + "x" + lastCam.pixelHeight * settings.captureSizeMultiplier;
    }

    private void DebugLogExtended(string log)
    {
        Debug.Log("[" + GetType() + "] " + log);
    }

    private void postCapture()
    {
        if (settings.includeCounter)
        {
            settings.counter++;
        }
    }
}