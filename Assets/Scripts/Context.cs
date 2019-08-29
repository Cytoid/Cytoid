using System;
using System.IO;
using Newtonsoft.Json;
using Proyecto26;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Object = System.Object;

public class Context : SingletonMonoBehavior<Context>
{
    public const string ApiBaseUrl = "https://api.cytoid.io";
    public const string WebsiteUrl = "https://cytoid.io";
    
    public const int ReferenceWidth = 1920;
    public const int ReferenceHeight = 1080;

    public static string DataPath;
    public static AudioManager AudioManager;
    public static ScreenManager ScreenManager;
    public static LevelManager LevelManager = new LevelManager();
    public static SpriteCache SpriteCache = new SpriteCache();

    public static Level SelectedLevel;
    public static Difficulty SelectedDifficulty = Difficulty.Easy;
    public static Difficulty PreferredDifficulty = Difficulty.Easy;
    public static Game CurrentGame;
    
    public static LocalPlayer LocalPlayer = new LocalPlayer();
    public static OnlinePlayer OnlinePlayer = new OnlinePlayer();

    protected override void Awake()
    {
        base.Awake();

        if (GameObject.FindGameObjectsWithTag("Context").Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        InitializeApplication();
    }

    private void InitializeApplication()
    {
        UnityEngine.Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Application.targetFrameRate = 120;

        DataPath = Application.persistentDataPath;
        print("Data path: " + DataPath);

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
#endif

#if UNITY_EDITOR
        Application.runInBackground = true;
#endif

        if (SceneManager.GetActiveScene().name == "Game")
        {
            // Load test level
            var level = LevelManager.LoadTestLevel();

            var game = (Game) FindObjectOfType(typeof(Game));
            game.Initialize(level, Difficulty.Parse(level.Meta.charts[0].type));
        }
    }

    public static void SetAutoRotation(bool autoRotation)
    {
        if (autoRotation)
        {
            UnityEngine.Screen.autorotateToLandscapeLeft = true;
            UnityEngine.Screen.autorotateToLandscapeRight = true;
        }
        else
        {
            if (UnityEngine.Screen.orientation != ScreenOrientation.LandscapeLeft)
                UnityEngine.Screen.autorotateToLandscapeLeft = false;
            if (UnityEngine.Screen.orientation != ScreenOrientation.LandscapeRight)
                UnityEngine.Screen.autorotateToLandscapeRight = false;
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
}