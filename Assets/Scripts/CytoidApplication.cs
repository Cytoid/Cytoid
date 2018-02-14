using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using LunarConsolePlugin;
using LunarConsolePluginInternal;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CytoidApplication : SingletonMonoBehavior<CytoidApplication>
{

	public static List<Level> Levels = new List<Level>();
	public static Level CurrentLevel;
	public static string CurrentChartType;
	public static LevelSelectionController.HitSound CurrentHitSound;
	public static PlayData CurrentPlayData;
	public static PlayResult LastPlayResult;

	public static string DataPath;
	[HideInInspector] public static bool UseDoozyUI;

	public static Texture2D backgroundTexture;

	private void Awake()
	{
		if (GameObject.FindGameObjectsWithTag("ApplicationObject").Length > 1)
		{
			Destroy(gameObject);
			return;
		}
		DontDestroyOnLoad(gameObject);
		Application.targetFrameRate = 60;
		ZPlayerPrefs.Initialize(SecuredConstants.password, SecuredConstants.salt);
		UseDoozyUI = Type.GetType("DoozyUI.UIElement") != null;
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
#endif

		backgroundTexture = new Texture2D(1024, 1024, TextureFormat.RGBA4444, false);
		backgroundTexture.Compress(true);
		
		ReloadLevels();
	}

	public static void DeleteLevel(Level level)
	{
		if (level.basePath != null)
		{
			Directory.Delete(Path.GetDirectoryName(level.basePath), true);
			Levels.Remove(level);
			SceneManager.LoadScene("LevelSelection");
		}
	}

	public static void ReloadLevels()
	{
		Levels.Clear();
		
		// Load levels
		var jsonFiles = Directory.GetFiles(DataPath, "level.json", SearchOption.AllDirectories).ToList();

		if (Application.platform != RuntimePlatform.Android)
		{
			jsonFiles.Add(Application.streamingAssetsPath + "/Glow Dance/level.json");
		}

		foreach (var jsonPath in jsonFiles){
			try
			{
				var info = new FileInfo(jsonPath);
				var basePath = info.Directory.FullName + "/";
				print(jsonPath);
				Level level;
				try
				{
					level = JsonConvert.DeserializeObject<Level>(File.ReadAllText(jsonPath));
				}
				catch (Exception e)
				{
					print(e.Message);
					continue;
				}
				if (level.id != null && level.id.Contains("io.cytoid"))
				{
					level.isInternal = true;
				}
				level.basePath = basePath;
				// LAG HERE
				level.charts.ForEach(chart => chart.LoadChart(level));
				// LAG HERE
				print(JsonConvert.SerializeObject(level));
				Levels.Add(level);
			}
			catch (Exception e)
			{
				print(e.Message);
				Log.e("Could not load " + jsonPath);
			}
		}

		try
		{
			if (Application.platform == RuntimePlatform.Android)
			{
				var path = Application.streamingAssetsPath + "/Glow Dance/level.json";
				var www = new WWW(path);
				while (!www.isDone)
				{
				}
				Level level;
				level = JsonConvert.DeserializeObject<Level>(Encoding.UTF8.GetString(www.bytes));
				level.isInternal = true;
				level.basePath = Application.streamingAssetsPath + "/Glow Dance/";
				print(level.basePath);
				level.charts.ForEach(chart => chart.LoadChart(level));
				print(JsonConvert.SerializeObject(level));
				Levels.Add(level);
			}
		}
		catch (Exception e)
		{
			print(e.Message);
			Log.e("Could not load the internal level. Press 'Get levels' to get some levels!");
		}

		Levels.Sort((a, b) => string.Compare(a.title, b.title, StringComparison.OrdinalIgnoreCase));

		if (PlayerPrefs.HasKey("last_level"))
		{
			var lastLevel = PlayerPrefs.GetString("last_level");
			foreach (var level in Levels)
			{
				if (level.id == lastLevel) CurrentLevel = level;
			}
		}
	}

	public static void SetAutoRotation(bool autoRotation)
	{
		Screen.autorotateToLandscapeLeft = autoRotation;
		Screen.autorotateToLandscapeRight = autoRotation;
	}

	public static AudioClip ReadAudioClipFromWWW(WWW www)
	{
		#if UNITY_EDITOR
			return www.GetAudioClip(false, true);
		#endif
		#if UNITY_IOS
		 	return www.GetAudioClip(false, true);
		#endif
		// Fallback
		return www.GetAudioClip(false, true);
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
						path = activityClass.Call<AndroidJavaObject>("getAndroidStorageFile").Call<string>("getAbsolutePath");
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
		    StartCoroutine(ExtractZipFile(Path.GetFileName(levelFile), zipFileData, DataPath + "/" + Path.GetFileNameWithoutExtension(levelFile)));
			while (extracting) yield return null;
			File.Delete(levelFile);
		}
		ReloadLevels();
		LevelsInstalling = false;
	}

	private bool extracting;
	
	public IEnumerator ExtractZipFile(string fileName, byte[] zipFileData, string targetDirectory, int bufferSize = 256 * 1024)
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
					Log.e("Cannot extract " + entry.Name + ". Is the .zip archive file valid?");
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
