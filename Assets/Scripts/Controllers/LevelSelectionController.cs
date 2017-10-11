﻿﻿using System;
using System.Collections;
using System.IO;
using System.Linq;
using SimpleUI.ScrollExtensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectionController : SingletonMonoBehavior<LevelSelectionController>
{
    public Level LoadedLevel { get; private set; }
    [HideInInspector] public DynamicScrollPoint WillScrollTo;

    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AlphaMask alphaMask;
    [SerializeField] private CanvasGroup listCanvasGroup;
    [SerializeField] private RectTransform scrollRectTransform;
    [SerializeField] private RectTransform listRectTransform;
    [SerializeField] private SwitchDifficultyView switchDifficultyView;
    [SerializeField] private GameObject blackout;
    [SerializeField] private Text blackoutText;
    
    [SerializeField] private Text idText;
    [SerializeField] private Text artistText;
    [SerializeField] private Text illustratorText;
    [SerializeField] private Text charterText;
    [SerializeField] private Text bestText;
    
    [SerializeField] private Toggle overrideOptionsToggle;
    [SerializeField] private InputField localUserOffsetInput;
    [SerializeField] private Toggle localIsInversedToggle;

    [SerializeField] private InputField userOffsetInput;
    [SerializeField] private Toggle showScannerToggle;
    [SerializeField] private Toggle isInversedToggle;
    [SerializeField] private InputField ringColorInput;
    [SerializeField] private InputField ringColorAltInput;
    [SerializeField] private InputField fillColorInput;
    [SerializeField] private InputField fillColorAltInput;

    private ScrollFocusController listScrollFocusController
    {
        get { return scrollRectTransform.GetComponent<ScrollFocusController>(); }
    }

    private string action = "";

    public static class Action
    {
        public const string Go = "Go";
        public const string Options = "Options";
        public const string Back = "Back";
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus) // Resuming
        {
            StartCoroutine(DetectNotInstalledLevels());
        }
    }

    public IEnumerator DetectNotInstalledLevels()
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            if (Directory.Exists(CytoidApplication.DataPath + "/Inbox/"))
            {
                foreach (var file in Directory.GetFiles(CytoidApplication.DataPath + "/Inbox/", "*.cytoidlevel"))
                {
                    var toPath = CytoidApplication.DataPath + "/" + Path.GetFileName(file);
                    if (File.Exists(toPath))
                    {
                        File.Delete(toPath);
                    }
                    File.Move(file, toPath);
                }
            }
        }
        string[] levelFiles;
        try
        {
            levelFiles =
                Directory.GetFiles(CytoidApplication.DataPath, "*.cytoidlevel");
        }
        catch (Exception)
        {
            // Ignored
            yield break;
        }
        if (levelFiles.Length > 0)
        {
            blackout.SetActive(true);
            StartCoroutine(CytoidApplication.Instance.InstallLevels(levelFiles));
            StartCoroutine(BlackoutTextAnim());
            while (CytoidApplication.Instance.LevelsInstalling) yield return null;
            if (false) // TODO: See RefreshLevels()
            {
                RefreshLevels();
                blackout.SetActive(false);
                // Scroll to first
                WillScrollTo = listScrollFocusController.first as DynamicScrollPoint;
            }
            else
            {
                SceneManager.LoadScene(gameObject.scene.name);
            }
        }
    }

    private bool blackoutTextFadingIn;

    private IEnumerator BlackoutTextAnim()
    {
        blackoutText.text = "Importing new levels (" + CytoidApplication.Instance.LevelInstallationProgress + "/" +
                            CytoidApplication.Instance.LevelInstallationTotal + ")...";
        if (blackoutTextFadingIn)
        {
            blackoutText.AlterColor(a: 0.04f);
            if (blackoutText.color.a >= 1)
            {
                blackoutTextFadingIn = false;
            }
        }
        else
        {
            blackoutText.AlterColor(a: -0.04f);
            if (blackoutText.color.a <= 0)
            {
                blackoutTextFadingIn = true;
            }
        }
        yield return new WaitForSeconds(0.01f);
        yield return StartCoroutine(BlackoutTextAnim());
    }

    protected override void Awake()
    {
        base.Awake();

        CytoidApplication.SetAutoRotation(true);

        var userOffsetDef = Application.platform == RuntimePlatform.Android ? 0.12f : 0.2f;
        var ringColorDef = "#FFFFFF";
        var ringColorAltDef = "#FFFFFF";
        var fillColorDef = "#6699CC";
        var fillColorAltDef = "#FF3C38";
        
        SetDefaultPref("user_offset", userOffsetDef);
        SetDefaultPref("show_scanner", true);
        SetDefaultPref("inverse", false);
        SetDefaultPref("ring_color", ringColorDef);
        SetDefaultPref("ring_color_alt", ringColorAltDef);
        SetDefaultPref("fill_color", fillColorDef);
        SetDefaultPref("fill_color_alt", fillColorAltDef);
        
        userOffsetInput.text = PlayerPrefs.GetFloat("user_offset").ToString();
        showScannerToggle.isOn = PlayerPrefsExt.GetBool("show_scanner");
        isInversedToggle.isOn = PlayerPrefsExt.GetBool("inverse");
        ringColorInput.text = PlayerPrefs.GetString("ring_color");
        ringColorAltInput.text = PlayerPrefs.GetString("ring_color_alt");
        fillColorInput.text = PlayerPrefs.GetString("fill_color");
        fillColorAltInput.text = PlayerPrefs.GetString("fill_color_alt");
        
        userOffsetInput.onEndEdit.AddListener(text =>
        {
            float offset;
            if (!float.TryParse(text, out offset))
            {
                userOffsetInput.text = userOffsetDef.ToString();
            }
        });
        overrideOptionsToggle.onValueChanged.AddListener(selected =>
        {
            ZPlayerPrefs.SetBool(PreferenceKeys.WillOverrideOptions(CytoidApplication.CurrentLevel), selected);
        });
        localIsInversedToggle.onValueChanged.AddListener(selected =>
        {
            ZPlayerPrefs.SetBool(PreferenceKeys.WillInverse(CytoidApplication.CurrentLevel), selected);
        });
        localUserOffsetInput.onEndEdit.AddListener(text =>
        {
            float offset;
            if (!float.TryParse(text, out offset))
            {
                localUserOffsetInput.text = userOffsetInput.text;
            }
            else
            {
                ZPlayerPrefs.SetFloat(PreferenceKeys.NoteDelay(CytoidApplication.CurrentLevel), offset);
            }
        });
        ringColorInput.onEndEdit.AddListener(text =>
        {
            try
            {
                Convert.HexToColor(text);
            }
            catch (Exception)
            {
                ringColorInput.text = ringColorDef;
            }
        });
        ringColorAltInput.onEndEdit.AddListener(text =>
        {
            try
            {
                Convert.HexToColor(text);
            }
            catch (Exception)
            {
                ringColorAltInput.text = ringColorAltDef;
            }
        });
        fillColorInput.onEndEdit.AddListener(text =>
        {
            try
            {
                Convert.HexToColor(text);
            }
            catch (Exception)
            {
                fillColorInput.text = fillColorDef;
            }
        });
        fillColorAltInput.onEndEdit.AddListener(text =>
        {
            try
            {
                Convert.HexToColor(text);
            }
            catch (Exception)
            {
                fillColorAltInput.text = fillColorAltDef;
            }
        });

        // Initialize background
        blackout.SetActive(false);

        var backgrounds = GameObject.FindGameObjectsWithTag("Background");
        if (backgrounds.Length > 1) // Already have persisted background? (i.e. returning from Game/GameResult scene)
        {
            var localBackground = backgrounds.ToList().Find(it => it.scene == gameObject.scene);
            // Destroy local background
            Destroy(localBackground);
            // Setup the persisted background
            BackgroundCanvasHelper.SetupBackgroundCanvas(gameObject.scene);
        }
        else // Setup the local background
        {
            BackgroundCanvasHelper.SetupBackgroundCanvas(gameObject.scene);
        }
    }

    private void SetDefaultPref(string key, object value)
    {
        if (!PlayerPrefs.HasKey(key))
        {
            if (value is bool)
            {
                PlayerPrefsExt.SetBool(key, (bool) value);
            } else if (value is float)
            {
                PlayerPrefs.SetFloat(key, (float) value);
            } else if (value is int)
            {
                PlayerPrefs.SetInt(key, (int) value);
            } else if (value is string)
            {
                PlayerPrefs.SetString(key, (string) value);
            }
        }
    }

    private void Start()
    {
        RefreshLevels();
        StartCoroutine(DetectNotInstalledLevels());
    }

    public void RefreshLevels()
    {
        // TODO: List rect will fuck up on second invoke.

        // Expand the list height
        // Formula: (Height + Spacing）* (How many entries) + Extra padding
        listRectTransform.ChangeSizeDelta(y: (56 - 6) * CytoidApplication.Levels.Count + 1000);

        // Add level entries into the list
        for (var index = 0; index < CytoidApplication.Levels.Count; index++)
        {
            var level = CytoidApplication.Levels[index];

            var entryObject = Instantiate(entryPrefab, listRectTransform.transform);
            entryObject.AddComponent<LevelEntry>().Level = level;
            entryObject.GetComponent<Text>().text = level.title;

            var dynamicScrollPoint = entryObject.GetComponent<DynamicScrollPoint>();
            listScrollFocusController.focusPoints.Add(dynamicScrollPoint);
            if (index == 0) listScrollFocusController.first = dynamicScrollPoint;
            if (CytoidApplication.CurrentLevel == level)
                WillScrollTo = dynamicScrollPoint; // Automatically scroll to
        }

        // Reset list position
        listRectTransform.position = new Vector2(0, 0);

        ForceLayoutInitialization.Instance.Invalidate();
    }

    private void Update()
    {
        if (WillScrollTo != null)
        {
            WillScrollTo.Focus();
            listScrollFocusController.scrollMover.scrollController.CenterOn(WillScrollTo.Rect);
            WillScrollTo = null;

            ForceLayoutInitialization.Instance.Invalidate();
        }
        
        // Android
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit(); 
    }

    public void UpdateBestText()
    {
        if (LoadedLevel == null) return;
        if (Math.Abs(ZPlayerPrefs.GetFloat(
                         PreferenceKeys.BestScore(LoadedLevel, CytoidApplication.CurrentChartType),
                         defaultValue: -1) - (-1)) < 0.000001)
        {
            bestText.text = "NO HIGH SCORE YET";
        }
        else
        {
            bestText.text =
                "Score " + Mathf.CeilToInt(ZPlayerPrefs.GetFloat(
                    PreferenceKeys.BestScore(LoadedLevel, CytoidApplication.CurrentChartType),
                    0)).ToString("D6")
                + "   Acc. " +
                ZPlayerPrefs.GetFloat(
                    PreferenceKeys.BestAccuracy(LoadedLevel, CytoidApplication.CurrentChartType),
                    0).ToString("0.##") + "%";
        }

    }

    public IEnumerator LoadLevel(Level level)
    {
        if (WillScrollTo != null) yield return null;
        
        WillHideList();

        if (LoadedLevel == level) yield break;
        LoadedLevel = level;

        idText.text = level.id ?? "Unknown";
        artistText.text = level.artist ?? level.composer ?? "Unknown";
        illustratorText.text = level.illustrator ?? "Unknown";
        charterText.text = level.charter ?? "Unknown";

        audioSource.Stop();

        StartCoroutine(LoadBackground(level));
        StartCoroutine(LoadMusicPreview(level));
    }

    public IEnumerator LoadBackground(Level level)
    {
        alphaMask.willFadeIn = true;

        // Load background sprite
        var www = new WWW((level.isInternal && Application.platform == RuntimePlatform.Android ? "" : "file://") + level.basePath + level.background.path);
        yield return www;

        while (alphaMask.IsFading) yield return null;
        yield return null; // Wait an extra frame

        // if (www.texture == null) yield break;
        
        www.LoadImageIntoTexture(CytoidApplication.backgroundTexture);

        var backgroundSprite =
            Sprite.Create(CytoidApplication.backgroundTexture, new Rect(0, 0, CytoidApplication.backgroundTexture.width, CytoidApplication.backgroundTexture.height), new Vector2(0, 0));
        var background = GameObject.FindGameObjectWithTag("Background");
        background.GetComponent<Image>().sprite = backgroundSprite;
        // background.GetComponent<CanvasRenderer>().SetTexture(CytoidApplication.backgroundTexture);

        // Destroy(www.texture);
        www.Dispose();

        Resources.UnloadUnusedAssets();

        // Fill the screen by adapting to the aspect ratio
        background.GetComponent<AspectRatioFitter>().aspectRatio =
            (float) CytoidApplication.backgroundTexture.width / CytoidApplication.backgroundTexture.height;

        yield return null;

        alphaMask.willFadeOut = true;
    }

    public IEnumerator LoadMusicPreview(Level level)
    {
        // Load preview
        var www = new WWW((level.isInternal && Application.platform == RuntimePlatform.Android ? "" : "file://") + level.basePath + level.music_preview.path);
        yield return www;

        while (alphaMask.IsFading) yield return null;
        yield return null; // Wait an extra frame

        var clip = CytoidApplication.ReadAudioClipFromWWW(www);
        if (clip == null) yield break;

        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
        
        www.Dispose();

        OnLevelLoaded();
    }

    public void OnLevelLoaded()
    {
        switchDifficultyView.OnLevelLoaded();
        CytoidApplication.CurrentLevel = LoadedLevel;
        
        UpdateBestText();

        var useLocalOptions = ZPlayerPrefs.GetBool(PreferenceKeys.WillOverrideOptions(CytoidApplication.CurrentLevel), false);
        overrideOptionsToggle.isOn = useLocalOptions;

        localIsInversedToggle.isOn = isInversedToggle.isOn;
        localUserOffsetInput.text = userOffsetInput.text;

        if (useLocalOptions)
        {
            localIsInversedToggle.isOn =
                ZPlayerPrefs.GetBool(PreferenceKeys.WillInverse(CytoidApplication.CurrentLevel), false);
            localUserOffsetInput.text =
                ZPlayerPrefs.GetFloat(PreferenceKeys.NoteDelay(CytoidApplication.CurrentLevel),
                    PlayerPrefs.GetFloat("user_offset")).ToString();
        }
    }

    public void WillHideList()
    {
        if (hideListCoroutine != null) return;
        hideListCoroutine = HideListCoroutine();
        StartCoroutine(hideListCoroutine);
    }

    public void CancelHideList()
    {
        if (hideListCoroutine != null)
        {
            StopCoroutine(hideListCoroutine);
            hideListCoroutine = null;
        }
        listCanvasGroup.alpha = 1;
    }

    private IEnumerator hideListCoroutine;
    
    private IEnumerator HideListCoroutine()
    {
        yield return new WaitForSeconds(1.5f); 
        while (listCanvasGroup.alpha > 0)
        {
            listCanvasGroup.alpha -= 0.14f;
            yield return null;
        }
        hideListCoroutine = null;
    }

    public void DoAction()
    {
        switch (action)
        {
            case Action.Go:
                print("Loading Game scene.");

                PlayerPrefs.SetFloat("user_offset", float.Parse(userOffsetInput.text));
                PlayerPrefsExt.SetBool("show_scanner", showScannerToggle.isOn);
                PlayerPrefsExt.SetBool("inverse", isInversedToggle.isOn);
                PlayerPrefs.SetString("ring_color", ringColorInput.text);
                PlayerPrefs.SetString("ring_color_alt", ringColorAltInput.text);
                PlayerPrefs.SetString("fill_color", fillColorInput.text);
                PlayerPrefs.SetString("fill_color_alt", fillColorAltInput.text);
                
                BackgroundCanvasHelper.PersistBackgroundCanvas();
                SceneManager.LoadScene("Game");
                break;
        }
    }

    public void OpenAboutPage()
    {
        Application.OpenURL("https://cytoid.io");
    }

    public void SetAction(string action)
    {
        this.action = action;
    }

    public class LevelEntry : MonoBehaviour
    {
        public Level Level;
    }
    
}