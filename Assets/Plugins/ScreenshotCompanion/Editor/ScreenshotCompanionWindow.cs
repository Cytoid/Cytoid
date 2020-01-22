#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using ScreenshotCompanionFramework;

public class ScreenshotCompanionWindow : EditorWindow
{
    List<CameraObject> list = new List<CameraObject>();

    bool foldoutSettings = false;
    Color signatureColor = new Color(1f, 0f, 0.5f);

    // image settings
    float renderSizeMultiplier = 1f;
    int captureSizeMultiplier = 1;
    Vector2 cutoutPosition;
    Vector2 cutoutSize;
    GUIStyle cutoutBoxStyle = null;
    string customName = "";
    string customDirectory = "";

    // name settings
    int lastCamID = 0;
    Camera lastCam;
    bool includeCamName = true;
    bool includeDate = true;
    bool includeResolution = true;

    bool singleCamera = false;

    bool applicationPath = false;

    // type settings
    enum FileType { PNG, JPG };
    FileType fileType;

    enum CaptureMethod { CaptureScreenshot, RenderTexture, Cutout };
    CaptureMethod captureMethod;

    // window menu entry
    [MenuItem("Window/Screenshot Companion")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(ScreenshotCompanionWindow));

        ScreenshotCompanionWindow window = EditorWindow.GetWindow<ScreenshotCompanionWindow>();
        GUIContent cont = new GUIContent("Screenshots");
        window.titleContent = cont;
    }

    void OnDisable()
    {
        refreshRequests();
    }

    // reset all X delete questions to standard
    void refreshRequests()
    {
        for (int i = 0; i < list.Count; i++)
        {
            list[i].deleteQuestion = false;
        }
    }

    void OnGUI()
    {
        GUI.color = signatureColor;
        if (GUILayout.Button("TOGGLE SETTINGS", "toolbarButton", GUILayout.MaxWidth(120), GUILayout.MinWidth(120)))
        {
            refreshRequests();
            foldoutSettings = !foldoutSettings;
        }

        GUI.color = Color.white;

        if (foldoutSettings)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("CAPTURE SETTINGS", EditorStyles.boldLabel);

            GUI.color = signatureColor;
            captureMethod = (ScreenshotCompanionWindow.CaptureMethod)EditorGUILayout.EnumPopup("capture method", captureMethod);

            GUI.color = Color.white;
            if (captureMethod == ScreenshotCompanionWindow.CaptureMethod.Cutout)
            {
                EditorGUILayout.HelpBox("This capture method is not supported in the Screenshot Companion Window. Please create an instance on a GameObject in your Scene.", MessageType.Info);
            }
            else if (captureMethod == ScreenshotCompanionWindow.CaptureMethod.RenderTexture)
            {
                EditorGUILayout.HelpBox("This capture method creates a RenderTexture that captures any Camera's " +
                "output in a custom resolution. This method also creates the sharpest upscaled images, but it can only use a single Camera.", MessageType.Info);

                EditorGUILayout.BeginHorizontal();

                GUI.color = Color.white;
                renderSizeMultiplier = EditorGUILayout.Slider("Size Multiplyer (float)", renderSizeMultiplier, 0.1f, 10f);
                EditorGUILayout.LabelField(getResolution(), EditorStyles.boldLabel, GUILayout.MaxWidth(100));

                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("This capture method creates a screenshot that is upscaled by a rounded number multiplier. ", MessageType.Info);

                singleCameraToggle();

                EditorGUILayout.BeginHorizontal();

                GUI.color = Color.white;
                captureSizeMultiplier = EditorGUILayout.IntSlider("Size Multiplyer (int)", captureSizeMultiplier, 1, 10);
                EditorGUILayout.LabelField(getResolution(), EditorStyles.boldLabel, GUILayout.MaxWidth(100));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            if (captureMethod != ScreenshotCompanionWindow.CaptureMethod.Cutout)
            {
                EditorGUILayout.Space();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField("DIRECTORY SETTINGS", EditorStyles.boldLabel);

                customDirectory = EditorGUILayout.TextField("Custom Name", customDirectory);

                applicationPathToggle();

                GUI.color = Color.white;
                EditorGUILayout.SelectableLabel("Directory = " + getSaveDirectory(), GUILayout.MaxHeight(16));


                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();


                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField("FILE SETTINGS", EditorStyles.boldLabel);



                customName = EditorGUILayout.TextField("Custom Name", customName);

                fileName();

                EditorGUILayout.LabelField("File Name = " + getFileName(lastCamID));

                EditorGUILayout.Space();



                fileTypeGUI();

                EditorGUILayout.Space();

                EditorGUILayout.EndVertical();
            }

        }

        if (captureMethod != ScreenshotCompanionWindow.CaptureMethod.Cutout)
        {

            EditorGUILayout.Space();

            GUI.color = signatureColor;




            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUI.color = Color.white;

            GUILayout.Label("Cameras:", EditorStyles.boldLabel);

            for (int i = 0; i < list.Count; i++)
            {
                CameraObject c = list[i];

                GUI.color = Color.white;
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                list[i].cam = (GameObject)EditorGUILayout.ObjectField(list[i].cam, typeof(GameObject), true);

                EditorGUI.BeginDisabledGroup(captureMethod == ScreenshotCompanionWindow.CaptureMethod.Cutout && !EditorApplication.isPlaying);
                if (list[i].cam != null)
                {
                    if (GUILayout.Button("USE " + list[i].cam.name, new GUIStyle(EditorStyles.miniButtonLeft)))
                    {
                        refreshRequests();
                        if (captureMethod == CaptureMethod.RenderTexture)
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
                        else if (captureMethod == CaptureMethod.CaptureScreenshot)
                        {
                            CaptureScreenshots(i, false);
                        }

                        lastCam = list[lastCamID].cam.GetComponent<Camera>();
                    }
                }
                EditorGUI.EndDisabledGroup();

                // the delete button
                if (c.deleteQuestion)
                {
                    GUI.color = Color.red;
                    if (GUILayout.Button("YES?", new GUIStyle(EditorStyles.miniButtonRight), GUILayout.MaxWidth(45), GUILayout.MaxHeight(14)))
                    {
                        refreshRequests();
                        Delete(i);
                    }
                }
                else
                {
                    GUI.color = (Color.red + Color.white * 2f) / 3f;
                    if (GUILayout.Button("X", new GUIStyle(EditorStyles.miniButtonRight), GUILayout.MaxWidth(45), GUILayout.MaxHeight(14)))
                    {
                        refreshRequests();
                        RequestDelete(i);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();




            EditorGUILayout.Space();

            GUI.color = signatureColor;
            if (GUILayout.Button("ADD CAMERA", "toolbarButton", GUILayout.MaxWidth(120), GUILayout.MinWidth(120)))
            {
                refreshRequests();
                Create();
            }

            EditorGUILayout.Space();
        }
    }

    // create a more blurry screenshot if there are multiple cameras or no camera is found on the GameObject
    void CaptureScreenshots(int id, bool fallback)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].cam != null)
                list[i].cam.SetActive(false);
        }
        list[id].cam.SetActive(true);

        if (!Directory.Exists(Directory.GetCurrentDirectory() + "/Screenshots/"))
        {
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/Screenshots/");
        }

        string fileName = Directory.GetCurrentDirectory() + "/Screenshots/";

        fileName += getFileName(id);

        ScreenCapture.CaptureScreenshot(fileName, captureSizeMultiplier);

        if (fallback)
        {
            Debug.Log("Fallback to Application.CaptureScreenshot because a GameObject without Camera (or Camera group) was used. Screenshot saved to: " + fileName);
        }
        else
        {
            Debug.Log("Screenshot saved to: " + fileName);
        }
    }

    // create a sharp screenshot for a single Camera
    void CaptureRenderTexture(Camera attachedCam, int id)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].cam != null)
                list[i].cam.SetActive(false);
        }
        list[id].cam.SetActive(true);

        if (!Directory.Exists(Directory.GetCurrentDirectory() + "/Screenshots/"))
        {
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/Screenshots/");
        }

        string fileName = Directory.GetCurrentDirectory() + "/Screenshots/";

        fileName += getFileName(id);

        int resWidth = (int)(attachedCam.pixelWidth * renderSizeMultiplier);
        int resHeight = (int)(attachedCam.pixelHeight * renderSizeMultiplier);

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
    }

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
        }
    }

    string getFileName(int camID)
    {
        string fileName = "";

        // custom name
        if (customName != "")
        {
            fileName += customName;
        }
        else
        {
            string dp = Application.dataPath;
            string[] s;
            s = dp.Split("/"[0]);
            fileName += s[s.Length - 2];
        }

        // include cam name
        if (includeCamName)
        {
            fileName += "_";

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
        }

        // include date
        if (includeDate)
        {
            fileName += "_";

            fileName += DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        }

        // include resolution
        if (includeResolution)
        {
            fileName += "_";

            fileName += getResolution();
        }

        // select filetype
        if (fileType == FileType.JPG)
        {
            fileName += ".jpg";
        }
        else if (fileType == FileType.PNG)
        {
            fileName += ".png";
        }

        return fileName;
    }

    string getResolution()
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

        if (captureMethod == CaptureMethod.RenderTexture)
        {
            return (int)(lastCam.pixelWidth * renderSizeMultiplier) + "x" + (int)(lastCam.pixelHeight * renderSizeMultiplier);
        }

        return lastCam.pixelWidth * captureSizeMultiplier + "x" + lastCam.pixelHeight * captureSizeMultiplier;
    }

    void singleCameraToggle()
    {
        EditorGUILayout.BeginHorizontal();

        GUI.color = singleCamera ? (Color.grey + Color.white) / 2f : Color.white;
        if (GUILayout.Button("Single Camera", "toolbarButton"))
        {
            refreshRequests();
            singleCamera = true;
        }

        GUI.color = singleCamera ? Color.white : (Color.grey + Color.white) / 2f;
        if (GUILayout.Button("Multiple Cameras", "toolbarButton"))
        {
            refreshRequests();
            singleCamera = false;
        }

        EditorGUILayout.EndHorizontal();
    }

    void applicationPathToggle()
    {
        EditorGUILayout.BeginHorizontal();

        GUI.color = applicationPath ? Color.white : (Color.grey + Color.white) / 2f;
        if (GUILayout.Button("Unity Project", "toolbarButton"))
        {
            refreshRequests();
            applicationPath = false;
        }

        GUI.color = applicationPath ? (Color.grey + Color.white) / 2f : Color.white;
        if (GUILayout.Button("Persistent Path", "toolbarButton"))
        {
            refreshRequests();
            applicationPath = true;
        }

        EditorGUILayout.EndHorizontal();
    }

    void fileName()
    {
        EditorGUILayout.BeginHorizontal();

        GUI.color = includeCamName ? (Color.grey + Color.white) / 2f : Color.white;
        if (GUILayout.Button("Camera Name", "toolbarButton"))
        {
            includeCamName = !includeCamName;
        }

        GUI.color = includeDate ? (Color.grey + Color.white) / 2f : Color.white;
        if (GUILayout.Button("Date", "toolbarButton"))
        {
            includeDate = !includeDate;
        }

        GUI.color = includeResolution ? (Color.grey + Color.white) / 2f : Color.white;
        if (GUILayout.Button("Resolution", "toolbarButton"))
        {
            includeResolution = !includeResolution;
        }

        EditorGUILayout.EndHorizontal();
    }

    void fileTypeGUI()
    {
        EditorGUILayout.BeginHorizontal();

        GUI.color = fileType == ScreenshotCompanionWindow.FileType.JPG ? Color.white : (Color.grey + Color.white) / 2f;
        if (GUILayout.Button("PNG", "toolbarButton"))
        {
            refreshRequests();
            fileType = ScreenshotCompanionWindow.FileType.PNG;
        }

        GUI.color = fileType == ScreenshotCompanionWindow.FileType.JPG ? (Color.grey + Color.white) / 2f : Color.white;
        if (GUILayout.Button("JPG", "toolbarButton"))
        {
            refreshRequests();
            fileType = ScreenshotCompanionWindow.FileType.JPG;
        }

        EditorGUILayout.EndHorizontal();
    }

    string getSaveDirectory()
    {
        string pickDirectory = customDirectory != "" ? customDirectory : "Screenshots";

        if (applicationPath)
        { // path to a safe location depending on the platform
            return Application.persistentDataPath + "/" + pickDirectory + "/";
        }
        else
        { // path to Unity project main folder
            return Directory.GetCurrentDirectory() + "/" + pickDirectory + "/";
        }
    }
}

#endif