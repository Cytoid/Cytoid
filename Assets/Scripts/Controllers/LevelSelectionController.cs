﻿using System;
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
                    File.Move(file, CytoidApplication.DataPath + "/" + Path.GetFileName(file));
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

        if (PlayerPrefs.HasKey("user_offset"))
        {
            GetUserOffsetInputField().text = PlayerPrefs.GetFloat("user_offset").ToString();
        }
        if (PlayerPrefs.HasKey("show_scanner"))
        {
            GetShowScannerToggle().isOn = PlayerPrefsExt.GetBool("show_scanner");
        }

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

    private InputField GetUserOffsetInputField()
    {
        return GameObject.FindGameObjectWithTag("UserOffsetInput").GetComponent<InputField>();
    }
    
    private Toggle GetShowScannerToggle()
    {
        return GameObject.FindGameObjectWithTag("ShowScannerToggle").GetComponent<Toggle>();
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

        DynamicScrollPoint funWillScrollTo = null;

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
                funWillScrollTo = dynamicScrollPoint; // Automatically scroll to
        }

        // Reset list position
        listRectTransform.position = new Vector2(0, 0);

        ForceLayoutInitialization.Instance.Invalidate();
        
        if (funWillScrollTo != null)
        {
            funWillScrollTo.Focus();
            listScrollFocusController.scrollMover.scrollController.CenterOn(funWillScrollTo.Rect);
        }
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
    }

    public void LoadLevel(Level level)
    {
        WillHideList();
        
        if (LoadedLevel == level) return;
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
        yield return new WaitForSeconds(3); 
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

                PlayerPrefs.SetFloat("user_offset", float.Parse(GetUserOffsetInputField().text));
                PlayerPrefsExt.SetBool("show_scanner", GetShowScannerToggle().isOn);

                BackgroundCanvasHelper.PersistBackgroundCanvas();
                SceneManager.LoadScene("Game");
                break;
        }
    }

    public void OpenAboutPage()
    {
        Application.OpenURL("https://github.com/TigerHix/Cytoid");
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