using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Batchmode headless play-through gate: boots the Navigation scene, starts the
/// built-in debug level on the Easy chart, waits for the chart to run to
/// completion + dispose, then exits.
///
/// Unity -batchmode -projectPath ... -executeMethod DebugPlayThrough.Run
/// (do NOT pass -quit; this method calls EditorApplication.Exit after async work
/// completes. With -quit, Unity exits before EditorApplication.update fires.)
///
/// Prerequisite: launch with CYTOID_FLUTTER_HOST stripped from the scripting
/// defines (the committed Android/iOS defines enable bridge-embedded mode, which
/// disables the standalone debug navigation). Phase state is tracked in
/// SessionState because entering play mode triggers a domain reload that resets
/// statics and would otherwise re-arm the timeout clock.
/// </summary>
[InitializeOnLoad]
public static class DebugPlayThrough
{
    private const string ActiveKey = "DebugPlayThrough.Active";
    private const string PhaseKey = "DebugPlayThrough.Phase";
    private const string PhaseStartedKey = "DebugPlayThrough.PhaseStarted";
    private const string CompletedAtKey = "DebugPlayThrough.CompletedAt";

    private enum Phase { BootingNavigation, WaitingForLevel, Playing }

    private const float PhaseTimeoutSeconds = 240f;
    private const float PlayTimeoutSeconds = 420f;

    static DebugPlayThrough()
    {
        if (SessionState.GetBool(ActiveKey, false)) EditorApplication.update += Pump;
    }

    public static void Run()
    {
        SessionState.SetBool(ActiveKey, true);
        SetPhase(Phase.BootingNavigation);
        Debug.Log("[DebugPlayThrough] starting");
        EditorApplication.update += Pump;
    }

    private static Phase CurrentPhase =>
        Enum.TryParse(SessionState.GetString(PhaseKey, Phase.BootingNavigation.ToString()),
            out Phase phase) ? phase : Phase.BootingNavigation;

    private static DateTime PhaseStarted =>
        new DateTime(long.Parse(SessionState.GetString(PhaseStartedKey,
            DateTime.UtcNow.Ticks.ToString())));

    private static void SetPhase(Phase phase)
    {
        SessionState.SetString(PhaseKey, phase.ToString());
        SessionState.SetString(PhaseStartedKey, DateTime.UtcNow.Ticks.ToString());
    }

    private static void Pump()
    {
        try
        {
            var timeout = CurrentPhase == Phase.Playing ? PlayTimeoutSeconds : PhaseTimeoutSeconds;
            if ((DateTime.UtcNow - PhaseStarted).TotalSeconds > timeout)
            {
                Debug.LogError($"[DebugPlayThrough] TIMED OUT in phase {CurrentPhase}");
                Finish(1);
                return;
            }

            switch (CurrentPhase)
            {
                case Phase.BootingNavigation: PumpBootingNavigation(); break;
                case Phase.WaitingForLevel: PumpWaitingForLevel(); break;
                case Phase.Playing: PumpPlaying(); break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[DebugPlayThrough] crashed in phase {CurrentPhase}: {e}");
            Finish(1);
        }
    }

    private static void PumpBootingNavigation()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Navigation.unity");
            EditorApplication.isPlaying = true;
            return;
        }

        // DebugNavigationController disables itself under CYTOID_FLUTTER_HOST; the
        // batch run always targets the standalone flow.
        foreach (var behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (behaviour.GetType().Name != "DebugNavigationController") continue;
            navigationController = behaviour;
            SetPhase(Phase.WaitingForLevel);
            Debug.Log("[DebugPlayThrough] play mode entered, waiting for level");
            return;
        }
    }

    private static void PumpWaitingForLevel()
    {
        // StartGame is private; the easy button only becomes interactable once the
        // built-in level finished loading, so poll that instead.
        var button = GetField(navigationController, "easyButton") as UnityEngine.UI.Button;
        if (button == null || !button.interactable) return;

        var startGame = navigationController.GetType().GetMethod("StartGame",
            BindingFlags.NonPublic | BindingFlags.Instance);
        startGame?.Invoke(navigationController, new object[] { Difficulty.Easy });
        SetPhase(Phase.Playing);
        Debug.Log("[DebugPlayThrough] game started");
    }

    private static void PumpPlaying()
    {
        var game = UnityEngine.Object.FindFirstObjectByType<Game>();
        // During the Navigation→Game scene swap a stale Game can surface as
        // Unity-fake-null; touching .State on it throws.
        if (game == null || !game || game.State == null) return;
        if (!game.State.IsCompleted && !game.State.IsFailed)
        {
            if (CompletedAtTicks == 0) return;
        }
        else if (CompletedAtTicks == 0)
        {
            SessionState.SetString(CompletedAtKey, DateTime.UtcNow.Ticks.ToString());
            Debug.Log("[DebugPlayThrough] game finished, giving the async tail 5s");
        }

        // Keep pumping a few seconds so the async Complete() tail (dispose, result
        // emit) runs to its exception/completion before we exit.
        var completedAt = new DateTime(CompletedAtTicks);
        if ((DateTime.UtcNow - completedAt).TotalSeconds < 5) return;
        Finish(0);
    }

    private static long CompletedAtTicks
    {
        get
        {
            var stored = SessionState.GetString(CompletedAtKey, string.Empty);
            return stored.Length > 0 ? long.Parse(stored) : 0;
        }
    }

    private static MonoBehaviour navigationController;

    private static object GetField(object target, string name)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        return target.GetType().GetField(name, flags)?.GetValue(target);
    }

    private static void Finish(int exitCode)
    {
        SessionState.SetBool(ActiveKey, false);
        EditorApplication.update -= Pump;
        Debug.Log($"[DebugPlayThrough] exit {exitCode}");
        EditorApplication.Exit(exitCode);
    }
}
