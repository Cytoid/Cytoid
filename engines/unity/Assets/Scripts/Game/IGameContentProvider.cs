using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IGameContentProvider
{
    bool IsExternal { get; }
    Level Level { get; }
    Difficulty Difficulty { get; }
    LevelMeta.ChartSection ChartSection { get; }
    UniTask<string> LoadChartText();
    UniTask<AudioClip> LoadMusic();
    UniTask<string> LoadStoryboardText();
    void Dispose();
}
