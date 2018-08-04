using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Cytus2.Models;
using ICSharpCode.SharpZipLib.Zip;
using LunarConsolePluginInternal;
using Newtonsoft.Json;
using Unicache;
using Unicache.Plugin;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CytoidApplication : SingletonMonoBehavior<CytoidApplication>
{
    public const string Host = "https://cytoid.io:8443";
    public static IUnicache Cache;

    public static List<Level> Levels = new List<Level>();
    public static Level CurrentLevel;
    public static string CurrentChartType = Level.Hard;
    public static LevelSelectionController.HitSound CurrentHitSound;
    public static Play CurrentPlay;
    public static RankedPlayData CurrentRankedPlayData;
    public static UnrankedPlayData CurrentUnrankedPlayData;

    public static bool IsReloadingLevels = true;
    public static string LoadingLevelId;
    public static int LoadingLevelIndex;
    public static int TotalLevelsToLoad;

    public static string DataPath;
    [HideInInspector] public static bool UseDoozyUi;

    public static int OriginalWidth;
    public static int OriginalHeight;

    public static Texture2D BackgroundTexture;

    protected override void Awake()
    {
        base.Awake();

        if (Cache == null) Cache = new MemoryCache();
        Cache.Handler = new SimpleDownloadHandler();
        Cache.UrlLocator = new SimpleUrlLocator();
        Cache.CacheLocator = new SimpleCacheLocator();

        if (GameObject.FindGameObjectsWithTag("ApplicationObject").Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Application.targetFrameRate = 120;
        ZPlayerPrefs.Initialize(SecuredConstants.password, SecuredConstants.salt);
        UseDoozyUi = Type.GetType("DoozyUI.UIElement") != null;

        OriginalWidth = Screen.width;
        OriginalHeight = Screen.height;
    }

    private void Start()
    {
#if !UNITY_EDITOR
// On Android...
		if (Application.platform == RuntimePlatform.Android)
		{
			var dir = GetAndroidStoragePath();
			if (dir == null)
			{
				Application.Quit();
				return;
			}
			DataPath = dir + "/Cytoid";
			// Create an empty folder if it doesn't already exist
			Directory.CreateDirectory(DataPath);
		}
		else
		{
			DataPath = Application.persistentDataPath;
		}
		#endif

#if UNITY_EDITOR

        DataPath = Application.persistentDataPath;
        Application.runInBackground = true;

#endif

        BackgroundTexture = new Texture2D(1024, 1024, TextureFormat.RGBA4444, false);
        BackgroundTexture.Compress(true);

        if (SceneManager.GetActiveScene().name == "LevelSelection")
        {
            ReloadLevels(Application.streamingAssetsPath);
            RefreshCurrentLevel();
        }
        else if (SceneManager.GetActiveScene().name == "Intro")
        {
            var thread = new Thread(ReloadLevels);
            thread.Start(Application.streamingAssetsPath);
        }
    }

    public static void DeleteLevel(Level level)
    {
        if (level.BasePath != null)
        {
            Directory.Delete(Path.GetDirectoryName(level.BasePath), true);
            Levels.Remove(level);
            SceneManager.LoadScene("LevelSelection");
        }
    }

    public static readonly string[] InternalLevels = {"Intro", "Sky", "Glow Dance", "Ecstatic"};

    public static void ReloadLevels(object streamingPath)
    {
        IsReloadingLevels = true;
        LoadingLevelIndex = 0;
        Levels.Clear();

        // Load levels
        var jsonFiles = Directory.GetFiles(DataPath, "level.json", SearchOption.AllDirectories).ToList();
        var internalJsonFiles = new List<string>();
        
        if (Application.platform != RuntimePlatform.Android)
        {
            foreach (var internalLevel in InternalLevels)
            {
                internalJsonFiles.Add(string.Format(streamingPath + "/{0}/level.json", internalLevel));
            }
        }

        TotalLevelsToLoad = jsonFiles.Count;

        foreach (var jsonPath in jsonFiles)
        {
            LoadingLevelIndex++;
            try
            {
                LoadLevel(jsonPath, false);
            }
            catch (Exception e)
            {
                print(e.Message);
                Log.e("Could not load " + jsonPath);
            }
        }
        
        foreach (var jsonPath in internalJsonFiles)
        {
            LoadingLevelIndex++;
            try
            {
                LoadLevel(jsonPath, true);
            }
            catch (Exception e)
            {
                print(e.Message);
                Log.e("Could not load " + jsonPath);
            }
        }

        // Workaround for loading internal levels on Android
        try
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                foreach (var internalLevel in InternalLevels)
                {
                    var path = string.Format(streamingPath + "/{0}/level.json", internalLevel);
                    var www = new WWW(path);
                    while (!www.isDone)
                    {
                    }

                    Level level;
                    level = JsonConvert.DeserializeObject<Level>(Encoding.UTF8.GetString(www.bytes));
                    level.IsInternal = true;
                    level.BasePath = string.Format(Application.streamingAssetsPath + "/{0}/", internalLevel);
                    print(level.BasePath);
                    Levels.Add(level);
                }
            }
        }
        catch (Exception e)
        {
            print(e.Message);
            Log.e("Intenal levels could not be loaded. Please download levels from CytoidIO!");
        }

        Levels.Sort((a, b) => string.Compare(a.title, b.title, StringComparison.OrdinalIgnoreCase));

        IsReloadingLevels = false;
    }

    private static void LoadLevel(string jsonPath, bool isInternal)
    {
        var info = new FileInfo(jsonPath);
        var basePath = info.Directory.FullName + "/";
        Level level;
        try
        {
            level = JsonConvert.DeserializeObject<Level>(File.ReadAllText(jsonPath));
            if (isInternal) level.IsInternal = true;
        }
        catch (Exception e)
        {
            print(e.Message);
            return;
        }
        level.BasePath = basePath;

        LoadingLevelId = level.id;
        Levels.Add(level);
    }

    public static void SetAutoRotation(bool autoRotation)
    {
        if (autoRotation)
        {
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
        }
        else
        {
            if (Screen.orientation != ScreenOrientation.LandscapeLeft)
                Screen.autorotateToLandscapeLeft = false;
            if (Screen.orientation != ScreenOrientation.LandscapeRight)
                Screen.autorotateToLandscapeRight = false;
        }
    }

    private string GetAndroidStoragePath()
    {
        var path = "";
        if (Application.platform == RuntimePlatform.Android)
        {
            try
            {
                using (var javaClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    using (var activityClass = javaClass.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        path = activityClass.Call<AndroidJavaObject>("getAndroidStorageFile")
                            .Call<string>("getAbsolutePath");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Could not get Android storage path: " + e.Message);
            }
        }

        return path;
    }

    [HideInInspector] public bool LevelsInstalling;
    [HideInInspector] public int LevelInstallationProgress;
    [HideInInspector] public int LevelInstallationTotal;

    public IEnumerator InstallLevels(string[] levelFiles)
    {
        LevelsInstalling = true;
        LevelInstallationTotal = levelFiles.Length;
        LevelInstallationProgress = 0;
        foreach (var levelFile in levelFiles)
        {
            LevelInstallationProgress++;
            var zipFileData = File.ReadAllBytes(levelFile);
            StartCoroutine(ExtractZipFile(Path.GetFileName(levelFile), zipFileData,
                DataPath + "/" + Path.GetFileNameWithoutExtension(levelFile)));
            while (extracting) yield return null;
            File.Delete(levelFile);

            Level level;
            try
            {
                level = JsonConvert.DeserializeObject<Level>(
                    File.ReadAllText(DataPath + "/" + Path.GetFileNameWithoutExtension(levelFile) + "/level.json"));
            }
            catch (Exception e)
            {
                print(e.Message);
                continue;
            }

            PlayerPrefs.SetString("last_level", level.id);
        }

        ReloadLevels(Application.streamingAssetsPath);
        LevelsInstalling = false;

        RefreshCurrentLevel();
    }

    public static void RefreshCurrentLevel()
    {
        if (!PlayerPrefs.HasKey("last_level"))
        {
            PlayerPrefs.SetString("last_level", "io.cytoid.intro");
        }
        var lastLevel = PlayerPrefs.GetString("last_level");
        foreach (var level in Levels)
        {
            if (level.id == lastLevel) CurrentLevel = level;
        }
    }

    public static void ResetResolution()
    {
        Screen.SetResolution(OriginalWidth, OriginalHeight, true);
    }

    private bool extracting;

    public IEnumerator ExtractZipFile(string fileName, byte[] zipFileData, string targetDirectory,
        int bufferSize = 256 * 1024)
    {
        extracting = true;
        try
        {
            Directory.CreateDirectory(targetDirectory);
        }
        catch (Exception)
        {
            extracting = false;
        }

        if (!extracting) yield break;
        using (var fileStream = new MemoryStream())
        {
            ZipFile zipFile = null;
            try
            {
                fileStream.Write(zipFileData, 0, zipFileData.Length);
                fileStream.Flush();
                fileStream.Seek(0, SeekOrigin.Begin);

                zipFile = new ZipFile(fileStream);

                foreach (ZipEntry entry in zipFile)
                {
                    // Loop through to ensure the file is valid
                }
            }
            catch (Exception e)
            {
                Log.e("Cannot read " + fileName + ". Is it a valid .zip archive file?");
                Log.e(e.Message);
                extracting = false;
            }

            if (!extracting || zipFile == null) yield break;

            foreach (ZipEntry entry in zipFile)
            {
                var targetFile = Path.Combine(targetDirectory, entry.Name);
                if (entry.Name.Contains("__MACOSX")) continue; // Fucking macOS...
                print("Extracting " + entry.Name + "...");

                FileStream outputFile = null;

                try
                {
                    outputFile = File.Create(targetFile);
                }
                catch (Exception e)
                {
                    Log.e("Cannot extract " + entry.Name + ". Is it a valid .zip archive file?");
                    Log.e(e.Message);
                    extracting = false;
                }

                if (!extracting || outputFile == null) yield break;

                using (outputFile)
                {
                    if (entry.Size <= 0) continue;
                    var zippedStream = zipFile.GetInputStream(entry);
                    var dataBuffer = new byte[bufferSize];

                    int readBytes;
                    while ((readBytes = zippedStream.Read(dataBuffer, 0, bufferSize)) > 0)
                    {
                        outputFile.Write(dataBuffer, 0, readBytes);
                        outputFile.Flush();
                        yield return null;
                    }
                }
            }
        }

        extracting = false;
    }
}