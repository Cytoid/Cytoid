using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

public class LevelManager
{
    public readonly List<Level> LoadedLevels = new List<Level>();
    private HashSet<string> LoadedPaths = new HashSet<string>();

    public async UniTask<bool> UnpackLevelPackage(string packagePath, string destFolder)
    {
        const int bufferSize = 256 * 1024;
        ZipStrings.CodePage = Encoding.UTF8.CodePage;
        try
        {
            Directory.CreateDirectory(destFolder);
        }
        catch (Exception error)
        {
            Debug.LogError("Failed to create level folder.");
            Debug.LogError(error);
            return false;
        }

        var fileName = Path.GetFileName(packagePath);
        var zipFileData = File.ReadAllBytes(packagePath);
        using (var fileStream = new MemoryStream())
        {
            ZipFile zipFile;
            try
            {
                fileStream.Write(zipFileData, 0, zipFileData.Length);
                fileStream.Flush();
                fileStream.Seek(0, SeekOrigin.Begin);

                zipFile = new ZipFile(fileStream);

                foreach (ZipEntry entry in zipFile)
                {
                    // Loop through all files to ensure the zip is valid
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Cannot read {fileName}. Is it a valid .zip archive file?");
                Debug.LogError(e.Message);
                return false;
            }

            foreach (ZipEntry entry in zipFile)
            {
                var targetFile = Path.Combine(destFolder, entry.Name);
                if (entry.Name.Contains("__MACOSX")) continue; // Fucking macOS...
                Debug.Log("Extracting " + entry.Name + "...");

                FileStream outputFile;
                try
                {
                    outputFile = File.Create(targetFile);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Cannot extract {entry.Name} from {fileName}. Is it a valid .zip archive file?");
                    Debug.LogError(e.Message);
                    return false;
                }
                
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
                        await UniTask.Yield(); // Prevent blocking main thread
                    }
                }
            }
        }

        return true;
    }
    
    public async UniTask LoadAllFromDataPath(bool clearExisting = true)
    {
        if (clearExisting)
        {
            LoadedLevels.Clear();
            LoadedPaths.Clear();
            Context.SpriteCache.DisposeTagged("LocalLevelCoverThumbnail");
        }

        var jsonPaths = Directory.GetFiles(Context.DataPath, "level.json", SearchOption.AllDirectories).ToList();
        Debug.Log($"Found {jsonPaths.Count} levels");
        
        await LoadFromMetadataFiles(jsonPaths);
    }

    public async UniTask LoadFromMetadataFiles(List<string> jsonPaths)
    {
        for (var index = 0; index < jsonPaths.Count; index++)
        {
            var jsonPath = jsonPaths[index];
            if (LoadedPaths.Contains(jsonPath))
            {
                Debug.Log($"Warning: {jsonPath} is already loaded!");
                continue;
            }

            var info = new FileInfo(jsonPath);
            if (info.Directory == null) continue;

            var path = info.Directory.FullName + Path.DirectorySeparatorChar;
            var meta = JsonConvert.DeserializeObject<LevelMeta>(File.ReadAllText(jsonPath));
            
            // Sort charts
            meta.SortCharts();
            
            // Reject invalid level meta
            if (!meta.Validate()) continue;
            
            var level = new Level(path, meta, info.LastWriteTimeUtc, info.LastWriteTimeUtc);
            
            LoadedLevels.Add(level);
            LoadedPaths.Add(jsonPath);
            
            Debug.Log($"Loaded {index + 1}/{jsonPaths.Count}: {meta.id} ({path})");
        }

        LoadedLevels.Sort((a, b) => string.Compare(a.Meta.title, b.Meta.title, StringComparison.OrdinalIgnoreCase));

        // Generate thumbnails
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

            Debug.Log($"Thumbnail generated {index + 1}/{jsonPaths.Count}: {level.Meta.id} ({path})");
        }
    }
}