using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cytoid.UI;
using DG.Tweening;
using DoozyUI;
using E7.Native;
using LunarConsolePluginInternal;
using Newtonsoft.Json;
using QuickEngine.Extensions;
using SimpleUI.ScrollExtensions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectionController : SingletonMonoBehavior<LevelSelectionController>
{
    public Level LoadedLevel { get; private set; }
    public bool LoadedAvatar;
    [HideInInspector] public DynamicScrollPoint WillScrollTo;

    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AlphaMask alphaMask;
    [SerializeField] private CanvasGroup listCanvasGroup;
    [SerializeField] private RectTransform scrollRectTransform;
    [SerializeField] private RectTransform listRectTransform;
    [SerializeField] private GameObject blackout;
    [SerializeField] private Text blackoutText;

    [SerializeField] private GameObject deleteButton;

    [SerializeField] private InputField chartRelativeOffsetInput;
    [SerializeField] private InputField headsetOffsetInput;

    [SerializeField] private InputField mainOffsetInput;
    [SerializeField] private Toggle largerHitboxesToggle;
    [SerializeField] private Toggle earlyLateIndicatorToggle;
    [SerializeField] private Text hitSoundText;

    [SerializeField] private InputField usernameInput;
    [SerializeField] private InputField passwordInput;

    [SerializeField] private Text rankStatusText;

    private bool isLoggingIn;

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
        CytoidApplication.ResetResolution();

        var ringColorDef = "#FFFFFF";
        var ringColorAltDef = "#FFFFFF";
        var fillColorDef = "#6699CC";
        var fillColorAltDef = "#FF3C38";

        SetDefaultPref("main offset", Application.platform == RuntimePlatform.Android ? 0f : 0.1f);
        SetDefaultPref("headset offset", -0.05f);
        SetDefaultPref("show_scanner", true);
        SetDefaultPref("inverse", false);
        SetDefaultPref("ring_color", ringColorDef);
        SetDefaultPref("ring_color_alt", ringColorAltDef);
        SetDefaultPref("fill_color", fillColorDef);
        SetDefaultPref("fill_color_alt", fillColorAltDef);
        SetDefaultPref("hit_sound", "None");

        var list = HitSounds.ToList();
        list.Insert(0, new HitSound {Name = "None"});
        HitSounds = list.ToArray();
        UpdateHitSound(HitSounds[1], save: false);

        var userHitSound = PlayerPrefs.GetString("hit_sound");
        for (var index = 0; index < HitSounds.Length; index++)
        {
            if (HitSounds[index].Name == userHitSound)
            {
                UpdateHitSound(HitSounds[index]);
                HitSoundIndex = index;
            }
        }

        mainOffsetInput.text = PlayerPrefs.GetFloat("main offset").ToString();
        headsetOffsetInput.text = PlayerPrefs.GetFloat("headset offset").ToString();
        earlyLateIndicatorToggle.isOn = PlayerPrefsExt.GetBool("early_late_indicator");
        largerHitboxesToggle.isOn = PlayerPrefsExt.GetBool("larger_hitboxes");

        mainOffsetInput.onEndEdit.AddListener(text =>
        {
            float offset;
            if (!float.TryParse(text, out offset))
            {
                mainOffsetInput.text = PlayerPrefs.GetFloat("main offset").ToString();
            } else
            {
                PlayerPrefs.SetFloat("main offset", offset);
            }
        });
        chartRelativeOffsetInput.onEndEdit.AddListener(text =>
        {
            float offset;
            if (!float.TryParse(text, out offset))
            {
                chartRelativeOffsetInput.text = ZPlayerPrefs.GetFloat(PreferenceKeys.ChartRelativeOffset(CytoidApplication.CurrentLevel.id)).ToString();
            }
            else
            {
                ZPlayerPrefs.SetFloat(PreferenceKeys.ChartRelativeOffset(CytoidApplication.CurrentLevel.id), offset);
            }
        });
        headsetOffsetInput.onEndEdit.AddListener(text =>
        {
            float offset;
            if (!float.TryParse(text, out offset))
            {
                headsetOffsetInput.text = PlayerPrefs.GetFloat("headset offset").ToString();
            }
            else
            {
                PlayerPrefs.SetFloat("headset offset", offset);
            }
        });

        usernameInput.text = PlayerPrefs.GetString(PreferenceKeys.LastUsername());
        passwordInput.text = PlayerPrefs.GetString(PreferenceKeys.LastPassword());

        if (!PlayerPrefs.HasKey("ranked"))
        {
            PlayerPrefsExt.SetBool("ranked", false);
        }

        rankStatusText.text = OnlinePlayer.Authenticated && PlayerPrefsExt.GetBool("ranked") ? "On" : "Off";

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

        if (OnlinePlayer.Authenticated && OnlinePlayer.AvatarTexture != null)
        {
            LoadedAvatar = true;
        }

        if (Application.platform == RuntimePlatform.Android)
        {
            headsetOffsetInput.gameObject.SetActive(false);
            LayoutRebuilder.ForceRebuildLayoutImmediate(headsetOffsetInput.transform.parent.GetComponent<RectTransform>());
        }

        EventKit.Subscribe<string>("meta reloaded", OnLevelMetaReloaded);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EventKit.Unsubscribe<string>("meta reloaded", OnLevelMetaReloaded);
    }

    public void SwitchRankedMode()
    {
        if (isLoggingIn)
        {
            Popup.Make(this, "Now signing in, please wait...");
            return;
        }

        if (OnlinePlayer.Authenticated)
        {
            var ranked = PlayerPrefsExt.GetBool("ranked", false);
            ranked = !ranked;
            PlayerPrefsExt.SetBool("ranked", ranked);
            rankStatusText.text = ranked ? "On" : "Off";
            UpdateBestText();
            if (ranked && !PlayerPrefsExt.GetBool("dont_show_what_is_ranked_mode_again", false))
            {
                UIManager.ShowUiElement("WhatIsRankedModeBackground", "MusicSelection");
                UIManager.ShowUiElement("WhatIsRankedModeRoot", "MusicSelection");
            }

            if (ranked)
            {
                EventKit.Broadcast("reload rankings");
            }
        }
        else
        {
            UIManager.ShowUiElement("LoginRoot", "MusicSelection");
            UIManager.ShowUiElement("LoginBackground", "MusicSelection");
        }
    }

    public void DontShowWhatIsRankedModeAgain()
    {
        PlayerPrefsExt.SetBool("dont_show_what_is_ranked_mode_again", true);
    }

    private void SetDefaultPref(string key, object value)
    {
        if (!PlayerPrefs.HasKey(key))
        {
            if (value is bool)
            {
                PlayerPrefsExt.SetBool(key, (bool) value);
            }
            else if (value is float)
            {
                PlayerPrefs.SetFloat(key, (float) value);
            }
            else if (value is int)
            {
                PlayerPrefs.SetInt(key, (int) value);
            }
            else if (value is string)
            {
                PlayerPrefs.SetString(key, (string) value);
            }
        }
    }

    private IEnumerator Start()
    {
        CytoidApplication.RefreshCurrentLevel();
        RefreshLevels();
        Login(true);
        StartCoroutine(LoadAvatarCoroutine());
        yield return null; // Wait for one frame
        StartCoroutine(DetectNotInstalledLevels());
    }

    public void RefreshLevels()
    {
        // TODO: List rect will fuck up on second invoke.

        // Expand the list height
        // Formula: (Height + Spacing）* (How many entries) + Extra padding
        listRectTransform.ChangeSizeDelta(y: ((56 - 6) * CytoidApplication.Levels.Count) + 1000);

        // Add level entries into the list
        for (var index = 0; index < CytoidApplication.Levels.Count; index++)
        {
            var level = CytoidApplication.Levels[index];

            var entryObject = Instantiate(entryPrefab, listRectTransform.transform);
            entryObject.AddComponent<LevelEntry>().Level = level;
            var text = entryObject.GetComponent<Text>();
            if (!string.IsNullOrEmpty(level.title_localized))
            {
                text.supportRichText = true;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.lineSpacing = 0.4f;
                text.text = level.title + "\n<size=12>" + level.title_localized + "</size>";
            }
            else
            {
                text.text = level.title;
            }

            var entry = entryObject.AddComponent<LevelEntryComponent>();
            entry.Id = level.id;

            var dynamicScrollPoint = entryObject.GetComponent<DynamicScrollPoint>();
            listScrollFocusController.focusPoints.Add(dynamicScrollPoint);
            if (index == 0) listScrollFocusController.first = dynamicScrollPoint;
            if (CytoidApplication.CurrentLevel == level)
            {
                WillScrollTo = dynamicScrollPoint; // Automatically scroll to
            }
        }

        // Reset list position
        listRectTransform.position = new Vector2(0, 0);

        ForceLayoutInitialization.Instance.Invalidate();
    }

    public void OnLevelMetaReloaded(string levelId)
    {
        Popup.Make(this, "Updated level meta.");
        foreach (var entry in listRectTransform.gameObject.GetComponentsInChildren<LevelEntryComponent>())
        {
            if (entry.Id == levelId)
            {
                var level = CytoidApplication.Levels.Find(it => it.id == levelId);

                if (level == null) return;
                
                var text = entry.GetComponent<Text>();
                
                if (!string.IsNullOrEmpty(level.title_localized))
                {
                    text.supportRichText = true;
                    text.verticalOverflow = VerticalWrapMode.Overflow;
                    text.lineSpacing = 0.4f;
                    text.text = level.title + "\n<size=12>" + level.title_localized + "</size>";
                }
                else
                {
                    text.text = level.title;
                }

                return;
            }
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

        // Android
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
    }

    public void UpdateBestText()
    {
        if (LoadedLevel == null) return;
        BestScoreText.WillInvalidate = true;
    }

    public IEnumerator LoadLevel(Level level)
    {
        if (WillScrollTo != null) yield return null;

        WillHideList();

        if (LoadedLevel == level) yield break;
        LoadedLevel = level;

        audioSource.Stop();

        StartCoroutine(LoadBackground(level));
        StartCoroutine(LoadMusicPreview(level));
    }

    public IEnumerator LoadBackground(Level level)
    {
        alphaMask.willFadeIn = true;

        // Load background sprite
        var www = new WWW((level.IsInternal && Application.platform == RuntimePlatform.Android ? "" : "file://") +
                          level.BasePath + level.background.path);
        yield return www;

        while (alphaMask.IsFading) yield return null;
        yield return null; // Wait an extra frame

        www.LoadImageIntoTexture(CytoidApplication.BackgroundTexture);

        var backgroundSprite =
            Sprite.Create(CytoidApplication.BackgroundTexture,
                new Rect(0, 0, CytoidApplication.BackgroundTexture.width, CytoidApplication.BackgroundTexture.height),
                new Vector2(0, 0));
        var background = GameObject.FindGameObjectWithTag("Background");
        background.GetComponent<Image>().sprite = backgroundSprite;

        www.Dispose();

        Resources.UnloadUnusedAssets();

        // Fill the screen by adapting to the aspect ratio
        background.GetComponent<AspectRatioFitter>().aspectRatio =
            (float) CytoidApplication.BackgroundTexture.width / CytoidApplication.BackgroundTexture.height;

        yield return null;

        alphaMask.willFadeOut = true;
    }

    public IEnumerator LoadMusicPreview(Level level)
    {
        // Load preview
        var www = new WWW((level.IsInternal && Application.platform == RuntimePlatform.Android ? "" : "file://") +
                          level.BasePath + level.music_preview.path);
        yield return www;

        while (alphaMask.IsFading) yield return null;
        yield return null; // Wait an extra frame

        var clip = www.GetAudioClip(false, true);
        if (clip == null) yield break;

        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();

        www.Dispose();

        OnLevelLoaded();
    }

    public void OnLevelLoaded()
    {
        EventKit.Broadcast("level loaded");
        CytoidApplication.CurrentLevel = LoadedLevel;

        UpdateBestText();

        chartRelativeOffsetInput.text =
            ZPlayerPrefs.GetFloat(PreferenceKeys.ChartRelativeOffset(CytoidApplication.CurrentLevel.id), 0f).ToString();

        deleteButton.SetActive(!LoadedLevel.IsInternal);

        PlayerPrefs.SetString("last_level", LoadedLevel.id);

        if (PlayerPrefsExt.GetBool("ranked"))
        {
            EventKit.Broadcast("reload rankings");
        }

        StartCoroutine(OnlineMeta.FetchMeta(LoadedLevel.id));
    }

    public void DeleteCurrentLevel()
    {
        //DontDestroyOnLoad(GameObject.FindGameObjectWithTag("UIManager"));
        CytoidApplication.DeleteLevel(CytoidApplication.CurrentLevel);
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

    [Serializable]
    public struct HitSound
    {
        public string Name;
    }

    [SerializeField] protected HitSound[] HitSounds;
    protected int HitSoundIndex;

    public void NextHitSound()
    {
        HitSoundIndex++;
        if (HitSoundIndex == HitSounds.Length)
        {
            HitSoundIndex = 0;
        }

        UpdateHitSound(HitSounds[HitSoundIndex]);
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        var pointer = NativeAudio.Load("Hits/" + HitSounds[HitSoundIndex].Name + ".wav");
        pointer.Play();
#endif
    }

    public void PrevHitSound()
    {
        HitSoundIndex--;
        if (HitSoundIndex < 0)
        {
            HitSoundIndex = HitSounds.Length - 1;
        }

        UpdateHitSound(HitSounds[HitSoundIndex]);

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        var pointer = NativeAudio.Load("Hits/" + HitSounds[HitSoundIndex].Name + ".wav");
        pointer.Play();
#endif
    }

    public void UpdateHitSound(HitSound hitSound, bool save = true)
    {
        hitSoundText.text = hitSound.Name;
        CytoidApplication.CurrentHitSound = hitSound;
        if (save) PlayerPrefs.SetString("hit_sound", hitSound.Name);
    }
    
    public void HideListImmediately()
    {
        listCanvasGroup.DOFade(0, 0.5f);
    }

    public void DoAction()
    {
        switch (action)
        {
            case Action.Go:
                print("Loading Game scene.");

                PlayerPrefsExt.SetBool("early_late_indicator", earlyLateIndicatorToggle.isOn);
                PlayerPrefsExt.SetBool("larger_hitboxes", largerHitboxesToggle.isOn);

                BackgroundCanvasHelper.PersistBackgroundCanvas();

                SceneManager.LoadScene("CytusGame");
                break;
        }
    }

    public void DownloadMoreLevels()
    {
        Application.OpenURL("https://cytoid.io/browse/");
    }

    public void SupportOnPatreon()
    {
        Application.OpenURL("https://www.patreon.com/tigerhix");
    }

    public void ViewLevelOnIO()
    {
        Application.OpenURL("https://cytoid.io/browse/" + LoadedLevel.id);
    }

    public void ViewProfileOnIO()
    {
        Application.OpenURL("https://cytoid.io/profile/" + OnlinePlayer.Name);
    }

    public void OnProfilePressed()
    {
        if (isLoggingIn)
        {
            Popup.Make(this, "Now signing in, please wait...");
            return;
        }

        if (OnlinePlayer.Authenticated)
        {
            if (UIManager.GetUiElements("ProfileRoot", "MusicSelection")[0].isVisible)
            {
                UIManager.HideUiElement("ProfileRoot", "MusicSelection");
                UIManager.HideUiElement("ProfileBackground", "MusicSelection");
            }
            else
            {
                UIManager.ShowUiElement("ProfileRoot", "MusicSelection");
                UIManager.ShowUiElement("ProfileBackground", "MusicSelection");
                EventKit.Broadcast("reload player rankings");
            }
        }
        else
        {
            UIManager.ShowUiElement("LoginRoot", "MusicSelection");
            UIManager.ShowUiElement("LoginBackground", "MusicSelection");
        }
    }

    public void Login(bool auto = false)
    {
        if (isLoggingIn) return;
        if (!auto) // Manual login
        {
            if (usernameInput.text.IsNullOrEmpty())
            {
                Popup.Make(this, "Please enter username.");
                return;
            }

            if (passwordInput.text.IsNullOrEmpty())
            {
                Popup.Make(this, "Please enter password.");
                return;
            }

            var username = usernameInput.text.Trim().ToLower();
            var password = passwordInput.text.Trim();
            PlayerPrefs.SetString(PreferenceKeys.LastUsername(), username);
            PlayerPrefs.SetString(PreferenceKeys.LastPassword(), password);
            UIManager.ShowUiElement("LoggingInRoot", "MusicSelection");
            UIManager.ShowUiElement("LoggingInBackground", "MusicSelection");
        }

        isLoggingIn = true;
        StartCoroutine("LoginCoroutine");
    }

    public void Logout()
    {
        CancelLogin();
        PlayerPrefs.DeleteKey(PreferenceKeys.LastUsername());
        PlayerPrefs.DeleteKey(PreferenceKeys.LastPassword());
        CloseProfileWindows();

        OnlinePlayer.Invalidate();
        LoadedAvatar = false;
        Popup.Make(this, "Signed out.");

        PlayerPrefsExt.SetBool("ranked", false);
        rankStatusText.text = "Off";
        
        UpdateBestText();
    }

    public void CancelLogin()
    {
        StopCoroutine("LoginCoroutine");
        UIManager.HideUiElement("LoggingInRoot", "MusicSelection");
        UIManager.HideUiElement("LoggingInBackground", "MusicSelection");
        isLoggingIn = false;
    }

    public IEnumerator LoginCoroutine()
    {
        if (OnlinePlayer.Authenticated || !PlayerPrefs.HasKey(PreferenceKeys.LastUsername()) ||
            !PlayerPrefs.HasKey(PreferenceKeys.LastPassword()))
        {
            isLoggingIn = false;
            yield break;
        }

        // If not logged in previously
        yield return OnlinePlayer.Authenticate();
        CloseLoginWindows();
        isLoggingIn = false;

        var authenticationResult = OnlinePlayer.LastAuthenticationResult;

        switch (authenticationResult.status)
        {
            case 0:
                Popup.Make(this, "Signed in.");
                rankStatusText.text = PlayerPrefsExt.GetBool("ranked") ? "On" : "Off";
                StartCoroutine(LoadAvatarCoroutine());
                if (PlayerPrefsExt.GetBool("ranked"))
                {
                    EventKit.Broadcast("reload rankings");
                }
                BestScoreText.WillInvalidate = true;

                break;
            case -1:
                LoadedAvatar = true;
                Popup.Make(this, "Could not fetch player data.");
                break;
            case 1: // User not exist
                LoadedAvatar = true;
                Popup.Make(this, authenticationResult.message);
                PlayerPrefs.DeleteKey(PreferenceKeys.LastUsername());
                PlayerPrefs.DeleteKey(PreferenceKeys.LastPassword());
                usernameInput.text = "";
                passwordInput.text = "";
                break;
            case 2: // Incorrect password
                LoadedAvatar = true;
                Popup.Make(this, authenticationResult.message);
                PlayerPrefs.DeleteKey(PreferenceKeys.LastPassword());
                passwordInput.text = "";
                break;
        }
    }

    public void CloseLoginWindows()
    {
        UIManager.HideUiElement("LoggingInRoot", "MusicSelection");
        UIManager.HideUiElement("LoggingInBackground", "MusicSelection");
        UIManager.HideUiElement("LoginRoot", "MusicSelection");
        UIManager.HideUiElement("LoginBackground", "MusicSelection");
    }

    public void CloseProfileWindows()
    {
        UIManager.HideUiElement("ProfileRoot", "MusicSelection");
        UIManager.HideUiElement("ProfileBackground", "MusicSelection");
    }

    public IEnumerator LoadAvatarCoroutine()
    {
        if (!OnlinePlayer.Authenticated || (OnlinePlayer.Authenticated && OnlinePlayer.AvatarTexture != null))
        {
            yield break;
        }

        using (var www = new WWW(OnlinePlayer.AvatarUrl))
        {
            Debug.Log("Downloading avatar");

            yield return www;

            LoadedAvatar = true;

            if (!string.IsNullOrEmpty(www.error))
            {
                Log.e(www.error);
                Popup.Make(this, "Could not download avatar.");
                yield break;
            }

            Debug.Log("Downloaded avatar");

            var texture = www.texture;

            OnlinePlayer.AvatarTexture = texture;
        }
    }

    public void Register()
    {
        Application.OpenURL("https://cytoid.io/register");
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