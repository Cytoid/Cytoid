#if UNITY_EDITOR

using UnityEngine;
using System.Collections;
using UnityEditor;
using ScreenshotCompanionFramework;

// ScreenshotCompanion by The Topicbird - talk@thetopicbird.com

[CustomEditor(typeof(ScreenshotCompanion))]
public class ScreenshotCompanionEditor : Editor
{
    [SerializeField] ScreenshotCompanion script;

    void OnEnable()
    {
        script = (ScreenshotCompanion)target;
    }

    void OnDisable()
    {
        refreshRequests();
    }

    // reset all X questions to standard
    void refreshRequests()
    {
        for (int i = 0; i < script.list.Count; i++)
        {
            script.list[i].deleteQuestion = false;
        }
    }

    public override void OnInspectorGUI()
    {
        EditorUtility.SetDirty(target);

        EditorGUILayout.Space();

        GUI.color = script.settings.signatureColor;
        if (GUILayout.Button("TOGGLE SETTINGS", "toolbarButton", GUILayout.MaxWidth(120), GUILayout.MinWidth(120)))
        {
            refreshRequests();
            script.foldoutSettings = !script.foldoutSettings;
        }

        GUI.color = Color.white;

        if (script.foldoutSettings)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("CAPTURE SETTINGS", EditorStyles.boldLabel);

            GUI.color = script.settings.signatureColor;
            script.settings.captureMethod = (ScreenshotCompanion.CaptureMethod)EditorGUILayout.EnumPopup("capture method", script.settings.captureMethod);

            GUI.color = Color.white;
            if (script.settings.captureMethod == ScreenshotCompanion.CaptureMethod.Cutout)
            {
                EditorGUILayout.HelpBox("This capture method will take any part of the current Game View and save it pixel by pixel. Not supported in Edit Mode.", MessageType.Info);

                singleCameraToggle();

                GUI.color = Color.white;
                script.settings.cutoutPosition = EditorGUILayout.Vector2Field("Cutout Position", script.settings.cutoutPosition);
                script.settings.cutoutSize = EditorGUILayout.Vector2Field("Cutout Size", script.settings.cutoutSize);
            }
            else if (script.settings.captureMethod == ScreenshotCompanion.CaptureMethod.RenderTexture)
            {
                EditorGUILayout.HelpBox("This capture method creates a RenderTexture that captures any Camera's " +
                "output in a custom resolution. This method also creates the sharpest upscaled images, but it can only use a single Camera.", MessageType.Info);

                EditorGUILayout.BeginHorizontal();

                GUI.color = Color.white;
                script.settings.renderSizeMultiplier = EditorGUILayout.Slider("Size Multiplyer (float)", script.settings.renderSizeMultiplier, 0.1f, 10f);
                EditorGUILayout.LabelField(script.getResolution(), EditorStyles.boldLabel, GUILayout.MaxWidth(100));

                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("This capture method creates a screenshot that is upscaled by a rounded number multiplier. ", MessageType.Info);

                singleCameraToggle();

                EditorGUILayout.BeginHorizontal();

                GUI.color = Color.white;
                script.settings.captureSizeMultiplier = EditorGUILayout.IntSlider("Size Multiplyer (int)", script.settings.captureSizeMultiplier, 1, 10);
                EditorGUILayout.LabelField(script.getResolution(), EditorStyles.boldLabel, GUILayout.MaxWidth(100));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();


            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("DIRECTORY SETTINGS", EditorStyles.boldLabel);

            script.settings.customDirectoryName = EditorGUILayout.TextField("Custom Name", script.settings.customDirectoryName);

            applicationPathToggle();

            GUI.color = Color.white;
            EditorGUILayout.SelectableLabel("Directory = " + script.getSaveDirectory(), GUILayout.MaxHeight(16));


            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();


            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("FILE SETTINGS", EditorStyles.boldLabel);



            script.settings.customFileName = EditorGUILayout.TextField("Custom Name", script.settings.customFileName);

            fileName();

            EditorGUILayout.LabelField("File Name = " + script.getFileName(script.lastCamID));

            EditorGUILayout.Space();



            fileType();

            EditorGUILayout.Space();

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();

        GUI.color = script.settings.signatureColor;


        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        GUI.color = Color.white;

        GUILayout.Label("Cameras:", EditorStyles.boldLabel);

        for (int i = 0; i < script.list.Count; i++)
        {
            CameraObject c = script.list[i];

            GUI.color = Color.white;
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            script.list[i].cam = (GameObject)EditorGUILayout.ObjectField(script.list[i].cam, typeof(GameObject), true);

            EditorGUI.BeginDisabledGroup(script.list[i].cam == null);
            script.list[i].hotkey = (KeyCode)EditorGUILayout.EnumPopup(script.list[i].hotkey, GUILayout.MaxWidth(60));
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(script.settings.captureMethod == ScreenshotCompanion.CaptureMethod.Cutout && !EditorApplication.isPlaying);
            if (script.list[i].cam != null)
            {
                if (GUILayout.Button("USE " + script.list[i].cam.name, new GUIStyle(EditorStyles.miniButtonLeft)))
                {
                    refreshRequests();
                    if (script.settings.captureMethod == ScreenshotCompanion.CaptureMethod.RenderTexture)
                    {
                        Camera attachedCam = script.list[i].cam.GetComponent<Camera>();
                        if (attachedCam == null)
                        {
                            script.CaptureScreenshots(i, true);
                        }
                        else
                        {
                            script.CaptureRenderTexture(attachedCam, i);
                        }
                    }
                    else if (script.settings.captureMethod == ScreenshotCompanion.CaptureMethod.CaptureScreenshot)
                    {
                        script.CaptureScreenshots(i, false);
                    }
                    else
                    {
                        //script.settings.StartCoroutine(script.settings.CaptureCutout (i));
                        script.CaptureCutoutVoid(i);
                    }

                    script.lastCam = script.list[script.lastCamID].cam.GetComponent<Camera>();
                }
            }
            EditorGUI.EndDisabledGroup();

            // the delete button
            if (c.deleteQuestion)
            {
                GUI.color = script.settings.signatureColor;
                if (GUILayout.Button("YES?", new GUIStyle(EditorStyles.miniButtonRight), GUILayout.MaxWidth(45), GUILayout.MaxHeight(14)))
                {
                    refreshRequests();
                    script.Delete(i);
                }
            }
            else
            {
                GUI.color = (script.settings.signatureColor + Color.white * 2f) / 3f;
                if (GUILayout.Button("X", new GUIStyle(EditorStyles.miniButtonRight), GUILayout.MaxWidth(45), GUILayout.MaxHeight(14)))
                {
                    refreshRequests();
                    script.RequestDelete(i);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        GUI.color = script.settings.signatureColor;
        if (GUILayout.Button("ADD CAMERA", "toolbarButton", GUILayout.MaxWidth(120), GUILayout.MinWidth(120)))
        {
            refreshRequests();
            script.Create();
        }

        EditorGUILayout.Space();
    }

    void singleCameraToggle()
    {
        EditorGUILayout.BeginHorizontal();

        GUI.color = script.settings.singleCamera ? (Color.grey + Color.white) / 2f : Color.white;
        if (GUILayout.Button("Single Camera", "toolbarButton"))
        {
            refreshRequests();
            script.settings.singleCamera = true;
        }

        GUI.color = script.settings.singleCamera ? Color.white : (Color.grey + Color.white) / 2f;
        if (GUILayout.Button("Multiple Cameras", "toolbarButton"))
        {
            refreshRequests();
            script.settings.singleCamera = false;
        }

        EditorGUILayout.EndHorizontal();
    }

    void applicationPathToggle()
    {
        EditorGUILayout.BeginHorizontal();

        GUI.color = script.settings.applicationPath ? Color.white : (Color.grey + Color.white) / 2f;
        if (GUILayout.Button("Unity Project", "toolbarButton"))
        {
            refreshRequests();
            script.settings.applicationPath = false;
        }

        GUI.color = script.settings.applicationPath ? (Color.grey + Color.white) / 2f : Color.white;
        if (GUILayout.Button("Persistent Path", "toolbarButton"))
        {
            refreshRequests();
            script.settings.applicationPath = true;
        }

        EditorGUILayout.EndHorizontal();
    }

    void fileName()
    {
        EditorGUILayout.BeginHorizontal();

        GUI.color = script.settings.includeProject ? (Color.grey + Color.white) / 2f : Color.white;
        if (GUILayout.Button("Project", "toolbarButton"))
        {
            script.settings.includeProject = !script.settings.includeProject;
        }

        GUI.color = script.settings.includeCamera ? (Color.grey + Color.white) / 2f : Color.white;
        if (GUILayout.Button("Camera", "toolbarButton"))
        {
            script.settings.includeCamera = !script.settings.includeCamera;
        }

        GUI.color = script.settings.includeDate ? (Color.grey + Color.white) / 2f : Color.white;
        if (GUILayout.Button("Date", "toolbarButton"))
        {
            script.settings.includeDate = !script.settings.includeDate;
        }

        GUI.color = script.settings.includeResolution ? (Color.grey + Color.white) / 2f : Color.white;
        if (GUILayout.Button("Resolution", "toolbarButton"))
        {
            script.settings.includeResolution = !script.settings.includeResolution;
        }

        GUI.color = script.settings.includeCounter ? (Color.grey + Color.white) / 2f : Color.white;
        if (GUILayout.Button("Counter", "toolbarButton"))
        {
            script.settings.includeCounter = !script.settings.includeCounter;
        }

        GUI.color = Color.white;
        if (script.settings.includeCounter)
        {
            if (GUILayout.Button("= 0", "toolbarButton", GUILayout.MaxWidth(32)))
            {
                script.settings.counter = 0;
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    void fileType()
    {
        EditorGUILayout.BeginHorizontal();

        GUI.color = script.settings.fileType == ScreenshotCompanion.FileType.JPG ? Color.white : (Color.grey + Color.white) / 2f;
        if (GUILayout.Button("PNG", "toolbarButton"))
        {
            refreshRequests();
            script.settings.fileType = ScreenshotCompanion.FileType.PNG;
        }

        GUI.color = script.settings.fileType == ScreenshotCompanion.FileType.JPG ? (Color.grey + Color.white) / 2f : Color.white;
        if (GUILayout.Button("JPG", "toolbarButton"))
        {
            refreshRequests();
            script.settings.fileType = ScreenshotCompanion.FileType.JPG;
        }

        EditorGUILayout.EndHorizontal();
    }
}

#endif