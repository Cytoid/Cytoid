using System;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using Polyglot;
using UnityEngine;
using UnityEngine.Networking;

public sealed class FileGameContentProvider : IGameContentProvider
{
    private readonly Level level;
    private readonly Difficulty difficulty;
    private AudioClipLoader audioClipLoader;

    public bool IsExternal => false;
    public Level Level => level;
    public Difficulty Difficulty => difficulty;
    public LevelMeta.ChartSection ChartSection => level.Meta.GetChartSection(difficulty.Id);

    public FileGameContentProvider(Level level, Difficulty difficulty)
    {
        this.level = level ?? throw new ArgumentNullException(nameof(level));
        this.difficulty = difficulty ?? throw new ArgumentNullException(nameof(difficulty));
    }

    public async UniTask<string> LoadChartText()
    {
        var chartPath = "file://" + level.Path + ChartSection.path;
        using (var request = UnityWebRequest.Get(chartPath))
        {
            await request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
            {
                throw new Exception($"Failed to download chart from {chartPath}: {request.error}");
            }

            return Encoding.UTF8.GetString(request.downloadHandler.data);
        }
    }

    public async UniTask<AudioClip> LoadMusic()
    {
        var audioPath = "file://" + level.Path + level.Meta.GetMusicPath(difficulty.Id);
        audioClipLoader = new AudioClipLoader(audioPath);
        await audioClipLoader.Load();
        if (audioClipLoader.Error != null)
        {
            throw new Exception($"Failed to download audio from {audioPath}: {audioClipLoader.Error}");
        }

        return audioClipLoader.AudioClip;
    }

    public UniTask<string> LoadStoryboardText()
    {
        var storyboardPath = ResolveStoryboardPath();
        if (storyboardPath == null || !File.Exists(storyboardPath))
        {
            return UniTask.FromResult<string>(null);
        }

        return UniTask.FromResult(File.ReadAllText(storyboardPath));
    }

    private string ResolveStoryboardPath()
    {
        var chartMeta = ChartSection;
        string sbFile = null;
        if (chartMeta.storyboard != null)
        {
            if (chartMeta.storyboard.localizations != null)
            {
                chartMeta.storyboard.localizations.TryGetValue(Localization.Instance.SelectedLanguage.ToString(), out sbFile);
            }

            if (sbFile == null)
            {
                sbFile = chartMeta.storyboard.path;
            }
        }

        return level.Path + (sbFile ?? "storyboard.json");
    }

    public void Dispose()
    {
        audioClipLoader?.DisposeDecoder();
        audioClipLoader = null;
    }
}
