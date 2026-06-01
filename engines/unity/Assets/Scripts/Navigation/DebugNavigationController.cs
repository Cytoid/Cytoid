using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using File = System.IO.File;

public class DebugNavigationController : MonoBehaviour
{
    public const string DebugLevelId = "io.cytoid.8bit_adventurer";

    public Button easyButton;
    public Button hardButton;
    public Button extremeButton;
    public Text resultText;

    private Level debugLevel;
    private Font uiFont;

    private void Awake()
    {
        if (GameEmbedMode.IsBridgeEmbedded)
        {
            gameObject.SetActive(false);
            return;
        }

        uiFont = Resources.Load<Font>("Fonts/Nunito-Regular");
        EnsureTextFonts();
    }

    private async void Start()
    {
        if (GameEmbedMode.IsBridgeEmbedded)
        {
            return;
        }

        EnsureTextFonts();
        SetButtonsInteractable(false);
        RefreshResultText();

        await UniTask.WaitUntil(() => Context.IsInitialized);
        await LoadDebugLevel();
    }

    public void RefreshResultText()
    {
        if (resultText != null)
        {
            resultText.text = LastPlayResult.LoadJson();
            Debug.Log($"[DebugNavigation] Result text refreshed: {resultText.text}");
        }
    }

    private async UniTask LoadDebugLevel()
    {
        debugLevel = await Context.LevelManager.LoadOrInstallBuiltInLevel(DebugLevelId, LevelType.BuiltIn);
        if (debugLevel != null && IsInstalledLevelCorrupted(debugLevel))
        {
            Context.LevelManager.UnloadLevelsOfType(LevelType.BuiltIn);
            debugLevel = await Context.LevelManager.LoadOrInstallBuiltInLevel(DebugLevelId, LevelType.BuiltIn, true);
        }

        if (debugLevel == null)
        {
            if (resultText != null) resultText.text = $"Failed to load {DebugLevelId}";
            return;
        }

        BindDifficultyButton(easyButton, Difficulty.Easy);
        BindDifficultyButton(hardButton, Difficulty.Hard);
        BindDifficultyButton(extremeButton, Difficulty.Extreme);
    }

    private static bool IsInstalledLevelCorrupted(Level level)
    {
        if (level == null) return true;
        foreach (var chart in level.Meta.charts)
        {
            var musicPath = level.Path + level.Meta.GetMusicPath(chart.type);
            if (!File.Exists(musicPath) || new System.IO.FileInfo(musicPath).Length <= 0) return true;

            var chartPath = level.Path + chart.path;
            if (!File.Exists(chartPath) || new System.IO.FileInfo(chartPath).Length <= 0) return true;
        }

        return false;
    }

    private void BindDifficultyButton(Button button, Difficulty difficulty)
    {
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        var chart = debugLevel.Meta.charts.FirstOrDefault(it => it.type == difficulty.Id);
        if (chart == null)
        {
            SetButtonText(button, $"{difficulty.Id.ToUpperInvariant()} missing");
            button.interactable = false;
            return;
        }

        SetButtonText(button, $"{ToDisplayName(difficulty)} {chart.difficulty}");
        button.interactable = true;
        button.onClick.AddListener(() => StartGame(difficulty));
    }

    private void StartGame(Difficulty difficulty)
    {
        if (debugLevel == null) return;

        SetButtonsInteractable(false);
        GameLaunchBridge.StartDebugGame(debugLevel, difficulty);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (easyButton != null) easyButton.interactable = interactable;
        if (hardButton != null) hardButton.interactable = interactable;
        if (extremeButton != null) extremeButton.interactable = interactable;
    }

    private static void SetButtonText(Button button, string value)
    {
        var text = button.GetComponentInChildren<Text>();
        if (text != null) text.text = value;
    }

    private void EnsureTextFonts()
    {
        EnsureButtonTextFont(easyButton);
        EnsureButtonTextFont(hardButton);
        EnsureButtonTextFont(extremeButton);
        if (resultText != null && resultText.font == null) resultText.font = uiFont;
    }

    private void EnsureButtonTextFont(Button button)
    {
        if (button == null) return;

        var text = button.GetComponentInChildren<Text>();
        if (text != null && text.font == null) text.font = uiFont;
    }

    private static string ToDisplayName(Difficulty difficulty)
    {
        var id = difficulty.Id;
        return string.IsNullOrEmpty(id) ? "Unknown" : char.ToUpperInvariant(id[0]) + id.Substring(1);
    }
}
