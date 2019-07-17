using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;

public class LevelManager
{
    public List<Level> loadedLevels = new List<Level>();

    public async UniTask ReloadLocalLevels()
    {
        loadedLevels.Clear();

        // Load levels
        var jsonFiles = Directory.GetFiles(Context.dataPath, "level.json", SearchOption.AllDirectories).ToList();

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
            if (meta.charts.Any(it => it.type == Difficulty.Easy.id))
                sortedCharts.Add(meta.charts.Find(it => it.type == Difficulty.Easy.id));
            if (meta.charts.Any(it => it.type == Difficulty.Hard.id))
                sortedCharts.Add(meta.charts.Find(it => it.type == Difficulty.Hard.id));
            if (meta.charts.Any(it => it.type == Difficulty.Extreme.id))
                sortedCharts.Add(meta.charts.Find(it => it.type == Difficulty.Extreme.id));
            meta.charts = sortedCharts;
            
            // Reject invalid level meta
            if (!meta.Validate()) continue;

            var level = new Level(path, meta);
            
            loadedLevels.Add(level);
            
            Debug.Log($"Loaded {index + 1}/{jsonFiles.Count}: {meta.id} ({path})");
        }

        loadedLevels.Sort((a, b) => string.Compare(a.meta.title, b.meta.title, StringComparison.OrdinalIgnoreCase));

        for (var index = 0; index < loadedLevels.Count; index++)
        {
            var level = loadedLevels[index];

            if (File.Exists(level.path + ".thumbnail")) continue;
            var path = "file://" + level.path + level.meta.background.path;
            
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
                    UnityEngine.Object.Destroy(coverTexture);

                    File.WriteAllBytes(level.path + ".thumbnail", bytes);
                }
            }

            Debug.Log($"Thumbnail generated {index + 1}/{jsonFiles.Count}: {level.meta.id} ({path})");
        }
    }
}