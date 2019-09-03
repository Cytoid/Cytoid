using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

public class LevelManager
{
    public List<Level> LoadedLevels = new List<Level>();

    public Level LoadTestLevel()
    {
        var info = new FileInfo(Context.DataPath + "/player/");
        var path = info.Directory.FullName + Path.DirectorySeparatorChar;
        var meta = JsonConvert.DeserializeObject<LevelMeta>(File.ReadAllText(path + "level.json"));
        return new Level(path, meta, info.LastWriteTimeUtc, info.LastWriteTimeUtc);
    }
    
    public async UniTask ReloadLocalLevels()
    {
        LoadedLevels.Clear();

        // Load levels
        var jsonFiles = Directory.GetFiles(Context.DataPath, "level.json", SearchOption.AllDirectories).ToList();

        Debug.Log($"Found {jsonFiles.Count} levels");
        
        for (var index = 0; index < jsonFiles.Count; index++)
        {
            var jsonPath = jsonFiles[index];

            var info = new FileInfo(jsonPath);
            if (info.Directory == null) continue;

            var path = info.Directory.FullName + Path.DirectorySeparatorChar;
            var meta = JsonConvert.DeserializeObject<LevelMeta>(File.ReadAllText(jsonPath));
            
            // Sort charts
            var sortedCharts = new List<LevelMeta.ChartSection>();
            if (meta.charts.Any(it => it.type == Difficulty.Easy.Id))
                sortedCharts.Add(meta.charts.Find(it => it.type == Difficulty.Easy.Id));
            if (meta.charts.Any(it => it.type == Difficulty.Hard.Id))
                sortedCharts.Add(meta.charts.Find(it => it.type == Difficulty.Hard.Id));
            if (meta.charts.Any(it => it.type == Difficulty.Extreme.Id))
                sortedCharts.Add(meta.charts.Find(it => it.type == Difficulty.Extreme.Id));
            meta.charts = sortedCharts;
            
            // Reject invalid level meta
            if (!meta.Validate()) continue;
            
            var level = new Level(path, meta, info.LastWriteTimeUtc, info.LastWriteTimeUtc);
            
            LoadedLevels.Add(level);
            
            Debug.Log($"Loaded {index + 1}/{jsonFiles.Count}: {meta.id} ({path})");
        }

        LoadedLevels.Sort((a, b) => string.Compare(a.Meta.title, b.Meta.title, StringComparison.OrdinalIgnoreCase));

        for (var index = 0; index < LoadedLevels.Count; index++)
        {
            var level = LoadedLevels[index];

            if (File.Exists(level.Path + ".thumbnail")) continue;
            var path = "file://" + level.Path + level.Meta.background.path;
            
            using (var request = UnityWebRequestTexture.GetTexture(path))
            {
                await request.SendWebRequest();
                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.Log($"Cannot get background texture from {path}");
                    Debug.Log(request.error);
                }
                else
                {
                    var coverTexture = DownloadHandlerTexture.GetContent(request);
                    var ratio = coverTexture.width / 800f;
                    TextureScaler.scale(coverTexture, 800, (int) (coverTexture.height / ratio));
                    var bytes = coverTexture.EncodeToJPG();
                    Object.Destroy(coverTexture);

                    File.WriteAllBytes(level.Path + ".thumbnail", bytes);
                }
            }

            Debug.Log($"Thumbnail generated {index + 1}/{jsonFiles.Count}: {level.Meta.id} ({path})");
        }
    }
}