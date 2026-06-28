using System;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

public static class GameLaunchBridge
{
    public static void StartGame(string launchJson)
    {
        try
        {
            LoadGameScene(PrepareLaunchProvider(launchJson)).Forget();
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
            LoadGameScene(new ExternalGameContentProvider(payload)).Forget();
        }
        catch (Exception e)
        {
            GameResultBridge.EmitError(e);
        }
    }

    public static void StartDebugGame(Level level, Difficulty difficulty)
    {
        LoadGameScene(new FileGameContentProvider(level, difficulty)).Forget();
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

            var payload = new GameLaunchPayload
            {
                levelMetaJson = JsonConvert.SerializeObject(level.Meta),
                selectedDifficulty = difficulty.Id,
                assets = new GameLaunchAssets
                {
                    vfsUri = GameLaunchVfs.ToFileUri(level.Path),
                    chartPath = chart.path,
                    musicPath = level.Meta.GetMusicPath(difficulty.Id),
                    storyboardPath = chart.storyboard?.path
                }
            };

            StartGameWithPayload(payload);
        }
        catch (Exception e)
        {
            GameResultBridge.EmitError(e);
        }
    }

    internal static IGameContentProvider PrepareLaunchProvider(string launchJson)
    {
        return ExternalGameContentProvider.FromJson(launchJson);
    }

    internal static async UniTask LoadGameScene(IGameContentProvider provider, Action onLaunchFailed = null)
    {
        try
        {
            PrepareProviderContext(provider);
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
            onLaunchFailed?.Invoke();
        }
    }

    private static void PrepareProviderContext(IGameContentProvider provider)
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
    }
}
