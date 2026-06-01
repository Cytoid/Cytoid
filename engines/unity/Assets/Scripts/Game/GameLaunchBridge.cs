using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

public static class GameLaunchBridge
{
    public static void StartGame(string launchJson)
    {
        try
        {
            StartGameWithProvider(ExternalGameContentProvider.FromJson(launchJson)).Forget();
        }
        catch (Exception e)
        {
            GameResultBridge.EmitError(e);
        }
    }

    public static void StartGameWithPayload(GameLaunchPayload payload)
    {
        try
        {
            StartGameWithProvider(new ExternalGameContentProvider(payload)).Forget();
        }
        catch (Exception e)
        {
            GameResultBridge.EmitError(e);
        }
    }

    public static void StartDebugGame(Level level, Difficulty difficulty)
    {
        StartGameWithProvider(new FileGameContentProvider(level, difficulty)).Forget();
    }

    public static void StartDebugGameAsExternalPayload(Level level, Difficulty difficulty)
    {
        try
        {
            var chart = level.Meta.GetChartSection(difficulty.Id);
            if (chart == null)
            {
                throw new ArgumentException($"Missing chart for difficulty: {difficulty.Id}");
            }

            var storyboardPath = level.Path + (chart.storyboard?.path ?? "storyboard.json");
            var payload = new GameLaunchPayload
            {
                levelMetaJson = JsonConvert.SerializeObject(level.Meta),
                selectedDifficulty = difficulty.Id,
                chartText = File.ReadAllText(level.Path + chart.path),
                musicBytes = File.ReadAllBytes(level.Path + level.Meta.GetMusicPath(difficulty.Id)),
                musicFormat = Path.GetExtension(level.Meta.GetMusicPath(difficulty.Id)).TrimStart('.'),
                storyboardText = File.Exists(storyboardPath) ? File.ReadAllText(storyboardPath) : null
            };

            StartGameWithPayload(payload);
        }
        catch (Exception e)
        {
            GameResultBridge.EmitError(e);
        }
    }

    private static async UniTask StartGameWithProvider(IGameContentProvider provider)
    {
        try
        {
            Context.GameContentProvider?.Dispose();
            Context.GameContentProvider = provider;
            Context.SelectedLevel = provider.Level;
            Context.SelectedDifficulty = provider.Difficulty;
            Context.SelectedGameMode = GameMode.Standard;
            Context.SelectedMods.Clear();
            Context.PendingTierPlay = null;
            Context.ActiveTierPlaySession = null;

            if (provider is ExternalGameContentProvider externalProvider)
            {
                var gameModeStr = externalProvider.Payload.gameMode;
                if (!string.IsNullOrEmpty(gameModeStr) &&
                    Enum.TryParse(gameModeStr, true, out GameMode parsedMode))
                {
                    Context.SelectedGameMode = parsedMode;
                }
                Context.SelectedMods.UnionWith(externalProvider.ParseMods());
                externalProvider.ApplySettings();

                if (Context.SelectedGameMode == GameMode.Tier)
                {
                    if (externalProvider.Payload.tierPlay == null)
                    {
                        throw new ArgumentException("tierPlay is required when gameMode is Tier");
                    }

                    Context.PendingTierPlay = externalProvider.Payload.tierPlay;
                }
            }

            Context.GameState = null;
            Context.GameErrorState = null;

            var sceneLoader = new SceneLoader("Game");
            await sceneLoader.Load();
            sceneLoader.Activate();
        }
        catch (Exception e)
        {
            provider.Dispose();
            if (ReferenceEquals(Context.GameContentProvider, provider))
            {
                Context.GameContentProvider = null;
            }
            GameResultBridge.EmitError(e);
        }
    }
}
