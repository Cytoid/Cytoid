using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DoozyUI;
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
    [SerializeField] private Text titleText;
    [SerializeField] private Text artistText;
    [SerializeField] private Text illustratorText;
    [SerializeField] private Text charterText;
    [SerializeField] private Text bestText;
    [SerializeField] private Text confirmText;
    [SerializeField] private GameObject deleteButton;

    [SerializeField] private Toggle overrideOptionsToggle;
    [SerializeField] private InputField localUserOffsetInput;

    [SerializeField] private InputField userOffsetInput;
    [SerializeField] private Toggle largerHitboxesToggle;
    [SerializeField] private Toggle earlyLateIndicatorToggle;
    [SerializeField] private Text hitSoundText;
    [SerializeField] private AudioSource hitSoundPlayer;

    [SerializeField] private InputField usernameInput;
    [SerializeField] private InputField passwordInput;

    [SerializeField] private Image avatarImage;

    [SerializeField] private Text usernameText;
    [SerializeField] private Text rankText;

    [SerializeField] private Text modStatusText;
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
        SetDefaultPref("hit_sound", "None");

        var list = HitSounds.ToList();
        list.Insert(0, new HitSound {Name = "None", Clip = null});
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

        userOffsetInput.text = PlayerPrefs.GetFloat("user_offset").ToString();
        earlyLateIndicatorToggle.isOn = PlayerPrefsExt.GetBool("early_late_indicator");
        largerHitboxesToggle.isOn = PlayerPrefsExt.GetBool("larger_hitboxes");

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

        usernameInput.text = PlayerPrefs.GetString(PreferenceKeys.LastUsername());
        passwordInput.text = PlayerPrefs.GetString(PreferenceKeys.LastPassword());

        if (!PlayerPrefs.HasKey("ranked"))
        {
            PlayerPrefsExt.SetBool("ranked", false);
        }

        rankStatusText.text = "Off";

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

    public void SwitchRankedMode()
    {
        if (isLoggingIn)
        {
            Popup.Make(this, "Now signing in, please wait...");
            return;
        }

        if (User.Exists())
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
            if (level.title_localized != null)
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
        bool ranked = PlayerPrefsExt.GetBool("ranked");
        if (Math.Abs(ZPlayerPrefs.GetFloat(
                         PreferenceKeys.BestScore(LoadedLevel, CytoidApplication.CurrentChartType, ranked),
                         defaultValue: -1) - (-1)) < 0.000001)
        {
            bestText.text = "NO HIGH SCORE YET";
        }
        else
        {
            bestText.text =
                (ranked ? "R score" : "Score") + " " + Mathf.CeilToInt(ZPlayerPrefs.GetFloat(
                    PreferenceKeys.BestScore(LoadedLevel, CytoidApplication.CurrentChartType, ranked),
                    0)).ToString("D6")
                + "   " + (ranked ? "R acc." : "Acc.") + " " +
                ZPlayerPrefs.GetFloat(
                    PreferenceKeys.BestAccuracy(LoadedLevel, CytoidApplication.CurrentChartType, ranked),
                    0).ToString("0.##") + "%";
        }
    }

    public IEnumerator LoadLevel(Level level)
    {
        if (WillScrollTo != null) yield return null;

        WillHideList();

        if (LoadedLevel == level) yield break;
        LoadedLevel = level;

        idText.text = (level.id ?? "Unknown") + " (v" + level.version + ")";
        titleText.text = level.title ?? "Unknown";
        artistText.text = level.artist ?? "Unknown";
        illustratorText.text = "by " + (level.illustrator ?? "Unknown");
        charterText.text = "by " + (level.charter ?? "Unknown");

        audioSource.Stop();

        StartCoroutine(LoadBackground(level));
        StartCoroutine(LoadMusicPreview(level));
    }

    public IEnumerator LoadBackground(Level level)
    {
        alphaMask.willFadeIn = true;

        // Load background sprite
        var www = new WWW((level.is_internal && Application.platform == RuntimePlatform.Android ? "" : "file://") +
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
        var www = new WWW((level.is_internal && Application.platform == RuntimePlatform.Android ? "" : "file://") +
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
        switchDifficultyView.OnLevelLoaded();
        CytoidApplication.CurrentLevel = LoadedLevel;

        UpdateBestText();

        var useLocalOptions =
            ZPlayerPrefs.GetBool(PreferenceKeys.WillOverrideOptions(CytoidApplication.CurrentLevel), false);
        overrideOptionsToggle.isOn = useLocalOptions;

        if (useLocalOptions)
        {
            localUserOffsetInput.text =
                ZPlayerPrefs.GetFloat(PreferenceKeys.NoteDelay(CytoidApplication.CurrentLevel),
                    PlayerPrefs.GetFloat("user_offset")).ToString();
        }
        else
        {
            localUserOffsetInput.text = userOffsetInput.text;
        }

        // confirmText.text = "Are you sure you want to delete\n" + LoadedLevel.id + "?";

        deleteButton.SetActive(!LoadedLevel.is_internal);

        PlayerPrefs.SetString("last_level", LoadedLevel.id);
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
        public AudioClip Clip;
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
        hitSoundPlayer.PlayOneShot(HitSounds[HitSoundIndex].Clip);
    }

    public void PrevHitSound()
    {
        HitSoundIndex--;
        if (HitSoundIndex < 0)
        {
            HitSoundIndex = HitSounds.Length - 1;
        }

        UpdateHitSound(HitSounds[HitSoundIndex]);
        hitSoundPlayer.PlayOneShot(HitSounds[HitSoundIndex].Clip);
    }

    public void UpdateHitSound(HitSound hitSound, bool save = true)
    {
        hitSoundText.text = hitSound.Name;
        CytoidApplication.CurrentHitSound = hitSound;
        if (save) PlayerPrefs.SetString("hit_sound", hitSound.Name);
    }

    public void DoAction()
    {
        switch (action)
        {
            case Action.Go:
                print("Loading Game scene.");

                PlayerPrefs.SetFloat("user_offset", float.Parse(userOffsetInput.text));
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
        Application.OpenURL("https://cytoid.io/profile/" + User.Instance.username);
    }

    public void OnProfilePressed()
    {
        if (isLoggingIn)
        {
            Popup.Make(this, "Now signing in, please wait...");
            return;
        }

        if (User.Exists())
        {
            UIManager.ShowUiElement("ProfileRoot", "MusicSelection");
            UIManager.ShowUiElement("ProfileBackground", "MusicSelection");
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
        avatarImage.overrideSprite = null;
        User.reset();
        Popup.Make(this, "Signed out.");

        PlayerPrefsExt.SetBool("ranked", false);
        rankStatusText.text = "Off";
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
        if (!PlayerPrefs.HasKey(PreferenceKeys.LastUsername()) || !PlayerPrefs.HasKey(PreferenceKeys.LastPassword()))
        {
            isLoggingIn = false;
            yield break;
        }

        // If logged in previously
        if (User.Exists())
        {
            // Do nothing
        }
        else
        {
            Debug.Log("Logging in");
            var username = PlayerPrefs.GetString(PreferenceKeys.LastUsername());
            var password = PlayerPrefs.GetString(PreferenceKeys.LastPassword());

            var request = new UnityWebRequest(CytoidApplication.Host + "/auth", "POST") {timeout = 10};
            var bodyRaw =
                Encoding.UTF8.GetBytes("{\"user\": \"" + username + "\", \"password\": \"" + password + "\"}");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.Send();

            if (request.isNetworkError || request.isHttpError)
            {
                Log.e(request.responseCode.ToString());
                Log.e(request.error);
                Popup.Make(this, "Could not sign in.");
                CloseLoginWindows();
                isLoggingIn = false;
                yield break;
            }

            var body = request.downloadHandler.text;

            AuthenticationResult authenticationResult;
            try
            {
                authenticationResult = JsonConvert.DeserializeObject<AuthenticationResult>(body);
            }
            catch (Exception e)
            {
                Log.e(e.Message);
                Popup.Make(this, "Could not sign in.");
                CloseLoginWindows();
                isLoggingIn = false;
                yield break;
            }

            request.Dispose();

            if (authenticationResult.status != 0)
            {
                Popup.Make(this, authenticationResult.message);

                if (authenticationResult.status == 1)
                {
                    PlayerPrefs.DeleteKey(PreferenceKeys.LastUsername());
                    PlayerPrefs.DeleteKey(PreferenceKeys.LastPassword());
                    usernameInput.text = "";
                    passwordInput.text = "";
                }
                else if (authenticationResult.status == 2)
                {
                    PlayerPrefs.DeleteKey(PreferenceKeys.LastPassword());
                    passwordInput.text = "";
                }

                CloseLoginWindows();
                isLoggingIn = false;
                yield break;
            }

            Popup.Make(this, "Signed in.");

            var profile = User.Init(username, password, authenticationResult.avatarUrl);
            profile.localVersion++;
            profile.Save();
        }

        CloseLoginWindows();
        isLoggingIn = false;

        rankStatusText.text = PlayerPrefsExt.GetBool("ranked") ? "On" : "Off";

        UpdateProfileUi();
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

    public void UpdateProfileUi()
    {
        usernameText.text = "Player ID: " + User.Instance.username;
        StartCoroutine(LoadAvatarCoroutine());
    }

    public IEnumerator LoadAvatarCoroutine()
    {
        if (User.Exists() && User.Instance.avatarTexture != null)
        {
            // Update avatar from memory
            var texture = User.Instance.avatarTexture;

            var rect = new Rect(0, 0, texture.width, texture.height);
            var sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100);

            avatarImage.overrideSprite = sprite;

            yield return null;
        }

        using (var www = new WWW(User.Instance.avatarUrl))
        {
            yield return www;

            if (!string.IsNullOrEmpty(www.error))
            {
                Log.e(www.error);
                Popup.Make(this, "Could not load avatar.");
            }

            Debug.Log("Downloaded avatar");

            var texture = www.texture;

            User.Instance.avatarTexture = texture;

            var rect = new Rect(0, 0, texture.width, texture.height);
            var sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100);

            avatarImage.overrideSprite = sprite;
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