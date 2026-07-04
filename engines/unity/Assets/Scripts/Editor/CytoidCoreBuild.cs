using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;

/// <summary>
/// Unity editor and batchmode entry points for cytoid_game_core plugin artifacts only.
/// </summary>
public static class CytoidCoreBuild
{
    /// <summary>
    /// Android library module id for Unity-as-Library exports embedded in the Flutter app.
    /// Intentionally distinct from the Flutter applicationId (<see cref="FlutterHostApplicationId"/>).
    /// </summary>
    public const string FlutterHostLibraryApplicationIdentifier = "com.example.cytoid_flutter.unity";

    /// <summary>Flutter plugin package that owns the Android JNI callback.</summary>
    public const string FlutterHostApplicationId = "org.cytoid.gamecore";

    public const string FlutterHostDefineSymbol = "CYTOID_FLUTTER_HOST";

    /// <summary>
    /// Unity export output relative to the Unity project root (parent of Assets/).
    /// Override via EditorPrefs key <see cref="FlutterUnityLibraryPathPrefKey"/> if needed.
    /// </summary>
    public const string DefaultFlutterUnityLibraryRelativePath = "flutter_plugin/.cytoid_game_core/exports/android/unityLibrary";

    public const string DefaultFlutterUnityIOSRelativePath = "flutter_plugin/.cytoid_game_core/exports/ios/UnityLibrary";

    public const string FlutterAndroidArtifactsRelativePath =
        "flutter_plugin/.cytoid_game_core/artifacts/unity/android";

    public const string FlutterIOSArtifactsRelativePath =
        "flutter_plugin/.cytoid_game_core/artifacts/unity/ios";

    /// <summary>Scenes included in every Flutter plugin export (CoreHostBootstrap + Game).</summary>
    public static readonly string[] PluginBuildScenes =
    {
        "Assets/Scenes/CoreHostBootstrap.unity",
        "Assets/Scenes/Game.unity"
    };

    private const string FlutterPluginToolRelativeDir = "flutter_plugin/tool";
    private const string BuildUnityAarScript = "build_unity_aar.sh";
    private const string BuildUnityIosFrameworkScript = "build_unity_ios_framework.sh";

    private const string FlutterUnityLibraryPathPrefKey = "Cytoid.FlutterUnityLibraryRelativePath";
    private const string FlutterUnityIOSPathPrefKey = "Cytoid.FlutterUnityIOSRelativePath";

    private const int MenuPriorityBuildAndroid = 10;
    private const int MenuPriorityBuildIOS = 11;

    /// <summary>Upper bound on waiting for a switch-triggered recompile to finish.</summary>
    private const int ScriptCompilationTimeoutSeconds = 600;

    /// <summary>
    /// Time to wait after a switch before treating "isCompiling=false" as "no compile needed"
    /// rather than "compile hasn't been scheduled yet" (SwitchActiveBuildTarget is async).
    /// </summary>
    private const double ScriptCompilationGracePeriodSeconds = 2.0;

    // Pending-build state keys. Static delegate registrations are wiped by domain reloads
    // (which SwitchActiveBuildTarget triggers), so we persist build intent in EditorPrefs
    // and re-register the continuation via [DidReloadScripts].
    private const string PendingBuildActiveKey     = "CytoidCoreBuild.Pending.Active";
    private const string PendingBuildPlatformKey   = "CytoidCoreBuild.Pending.Platform";
    private const string PendingBuildOutputDirKey  = "CytoidCoreBuild.Pending.OutputDir";
    private const string PendingBuildPackageKey    = "CytoidCoreBuild.Pending.Package";
    private const string PendingBuildIosSdkKey     = "CytoidCoreBuild.Pending.IosSdk";
    private const string PendingBuildStartTicksKey = "CytoidCoreBuild.Pending.StartTicks";

    /// <summary>
    /// Batchmode: Unity -batchmode -projectPath ... -executeMethod CytoidCoreBuild.ExportAndroidLibraryForFlutter
    /// (do NOT pass -quit; this method calls EditorApplication.Exit(0) after async work completes.
    /// With -quit, Unity exits before EditorApplication.update fires and no build is produced.)
    /// </summary>
    public static void ExportAndroidLibraryForFlutter()
    {
        ExportAndroidLibraryForFlutter(ResolveFlutterUnityLibraryOutputPath());
    }

    /// <summary>
    /// Batchmode: Unity -batchmode -projectPath ... -executeMethod CytoidCoreBuild.ExportIOSLibraryForFlutter
    /// (do NOT pass -quit; this method calls EditorApplication.Exit(0) after async work completes.
    /// With -quit, Unity exits before EditorApplication.update fires and no build is produced.)
    /// </summary>
    public static void ExportIOSLibraryForFlutter()
    {
        ExportIOSLibraryForFlutter(ResolveFlutterUnityIOSOutputPath(), true);
    }

    /// <summary>
    /// Batchmode: exports the iOS UnityLibrary Xcode project without invoking xcodebuild.
    /// Used by CI when the Unity export runs in GameCI/Linux and framework packaging
    /// happens later on a macOS runner. Same -quit contract as above.
    /// </summary>
    public static void ExportIOSLibraryForFlutterWithoutPackaging()
    {
        ExportIOSLibraryForFlutter(ResolveFlutterUnityIOSOutputPath(), false);
    }

    [MenuItem("Cytoid/Build Android Plugin Artifacts", false, MenuPriorityBuildAndroid)]
    public static void BuildAndroidPluginArtifactsMenu()
    {
        ExportAndroidLibraryForFlutter();
    }

    [MenuItem("Cytoid/Build iOS Plugin Artifacts", false, MenuPriorityBuildIOS)]
    public static void BuildIOSPluginArtifactsMenu()
    {
        ExportIOSLibraryForFlutter();
    }

    private static void ExportAndroidLibraryForFlutter(string outputDirectory)
    {
        if (EditorPrefs.GetBool(PendingBuildActiveKey, false))
        {
            throw new InvalidOperationException(
                "A CytoidCoreBuild export is already in progress. Wait for it to finish "
                + $"or clear EditorPrefs key '{PendingBuildActiveKey}' manually if stuck.");
        }

        SetPendingBuildState("android", outputDirectory, packageFramework: false, iosSdk: "");
        SwitchToAndroid();
        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
        EditorUserBuildSettings.buildAppBundle = false;
        Directory.CreateDirectory(outputDirectory);
        EditorApplication.update += ContinuePendingBuild;
    }

    private static void ExportIOSLibraryForFlutter(string outputDirectory, bool packageFramework)
    {
        if (EditorPrefs.GetBool(PendingBuildActiveKey, false))
        {
            throw new InvalidOperationException(
                "A CytoidCoreBuild export is already in progress. Wait for it to finish "
                + $"or clear EditorPrefs key '{PendingBuildActiveKey}' manually if stuck.");
        }

        var iosSdk = ResolveIosXcodeSdk();
        SetPendingBuildState("ios", outputDirectory, packageFramework, iosSdk);
        SwitchToIOS();
        Directory.CreateDirectory(outputDirectory);
        EditorApplication.update += ContinuePendingBuild;
    }

    private static void SwitchToAndroid()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.Generic;
    }

    private static void SwitchToIOS()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
    }

    private static void RunIOSExport(
        string[] scenes,
        string locationPathName,
        string applicationIdentifier,
        string iosSdk,
        Action<string> onSuccess)
    {
        var previousApplicationIdentifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS);
        var previousDefineSymbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.iOS);
        var previousIosSdkVersion = PlayerSettings.iOS.sdkVersion;

        try
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, applicationIdentifier);
            PlayerSettings.iOS.sdkVersion = ResolveUnityIosSdkVersion(iosSdk);
            PlayerSettings.SetScriptingDefineSymbols(
                NamedBuildTarget.iOS,
                MergeDefineSymbols(previousDefineSymbols, new[] {FlutterHostDefineSymbol}));

            AssetDatabase.SaveAssets();

            var builtScenes = scenes.Where(File.Exists).ToArray();
            if (builtScenes.Length == 0)
            {
                throw new Exception("No build scenes found on disk.");
            }

            var options = new BuildPlayerOptions
            {
                scenes = builtScenes,
                locationPathName = locationPathName,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                LogBuildReportErrors(report, "iOS");
                throw new Exception(
                    $"iOS build failed: {report.summary.result}. "
                    + "See Console for build step errors.");
            }

            onSuccess(Path.GetFullPath(locationPathName));
        }
        finally
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, previousApplicationIdentifier);
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.iOS, previousDefineSymbols);
            PlayerSettings.iOS.sdkVersion = previousIosSdkVersion;
            AssetDatabase.SaveAssets();
        }
    }

    /// <summary>
    /// Xcode SDK used for Unity iOS exports. Defaults to device builds.
    /// Override with CYTOID_IOS_SDK=iphonesimulator for Simulator-only artifacts.
    /// </summary>
    private static string ResolveIosXcodeSdk()
    {
        var sdk = Environment.GetEnvironmentVariable("CYTOID_IOS_SDK");
        if (string.IsNullOrWhiteSpace(sdk))
        {
            return "iphoneos";
        }

        return sdk.Trim();
    }

    private static iOSSdkVersion ResolveUnityIosSdkVersion(string xcodeSdk)
    {
        return xcodeSdk.IndexOf("simulator", StringComparison.OrdinalIgnoreCase) >= 0
            ? iOSSdkVersion.SimulatorSDK
            : iOSSdkVersion.DeviceSDK;
    }

    private static void RunAndroidExport(
        string[] scenes,
        string locationPathName,
        string applicationIdentifier,
        Action<string> onSuccess)
    {
        var previousApplicationIdentifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        var previousUseCustomKeystore = PlayerSettings.Android.useCustomKeystore;
        var previousSplitApplicationBinary = PlayerSettings.Android.splitApplicationBinary;
        var previousBuildApkPerCpuArchitecture = PlayerSettings.Android.buildApkPerCpuArchitecture;
        var previousTargetArchitectures = PlayerSettings.Android.targetArchitectures;
        var previousDefineSymbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
        var previousExportAsGoogleAndroidProject = EditorUserBuildSettings.exportAsGoogleAndroidProject;
        var previousBuildAppBundle = EditorUserBuildSettings.buildAppBundle;

        try
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, applicationIdentifier);
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.splitApplicationBinary = false;
            PlayerSettings.Android.buildApkPerCpuArchitecture = false;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.SetScriptingDefineSymbols(
                NamedBuildTarget.Android,
                MergeDefineSymbols(previousDefineSymbols, new[] {FlutterHostDefineSymbol}));

            AssetDatabase.SaveAssets();

            var builtScenes = scenes.Where(File.Exists).ToArray();
            if (builtScenes.Length == 0)
            {
                throw new Exception("No build scenes found on disk.");
            }

            var options = new BuildPlayerOptions
            {
                scenes = builtScenes,
                locationPathName = locationPathName,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                LogBuildReportErrors(report, "Android");
                throw new Exception(
                    $"Android build failed: {report.summary.result}. "
                    + "See Console for build step errors.");
            }

            onSuccess(Path.GetFullPath(locationPathName));
        }
        finally
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, previousApplicationIdentifier);
            PlayerSettings.Android.useCustomKeystore = previousUseCustomKeystore;
            PlayerSettings.Android.splitApplicationBinary = previousSplitApplicationBinary;
            PlayerSettings.Android.buildApkPerCpuArchitecture = previousBuildApkPerCpuArchitecture;
            PlayerSettings.Android.targetArchitectures = previousTargetArchitectures;
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, previousDefineSymbols);
            EditorUserBuildSettings.exportAsGoogleAndroidProject = previousExportAsGoogleAndroidProject;
            EditorUserBuildSettings.buildAppBundle = previousBuildAppBundle;
            AssetDatabase.SaveAssets();
        }
    }

    private static bool IsScriptCompilationPending()
    {
        return EditorApplication.isCompiling || EditorApplication.isUpdating;
    }

    /// <summary>
    /// Invoked by Unity after every script recompile (including the one triggered by
    /// <see cref="SwitchToAndroid"/>/<see cref="SwitchToIOS"/>). Static delegate
    /// registrations (like our <see cref="EditorApplication.update"/> handler) are
    /// wiped by the domain reload, so this is the only way to resume the pending build.
    /// </summary>
    [DidReloadScripts]
    private static void RestorePendingBuildAfterDomainReload()
    {
        if (!EditorPrefs.GetBool(PendingBuildActiveKey, false)) return;
        Debug.Log("[CytoidCoreBuild] Domain reload detected; resuming pending build...");
        EditorApplication.update += ContinuePendingBuild;
    }

    /// <summary>
    /// Update callback that waits for script compilation to finish (or for the grace
    /// window to elapse if no compile was triggered), then runs the pending export.
    /// Registered both from the export entry points and from
    /// <see cref="RestorePendingBuildAfterDomainReload"/> — whoever fires first wins,
    /// and the unregister on first run prevents double-execution.
    /// </summary>
    /// <remarks>
    /// Empirically verified Unity 6 batchmode pitfalls (do not regress these):
    /// <list type="bullet">
    /// <item><description><c>-quit</c> exits the instant executeMethod returns;
    /// <c>EditorApplication.update</c> callbacks never fire. Invocation MUST omit -quit
    /// (CI uses game-ci/unity-builder <c>manualExit: true</c>).</description></item>
    /// <item><description>Static delegate registrations do NOT survive the domain reload
    /// triggered by <c>SwitchActiveBuildTarget</c>. Pending state must live in
    /// EditorPrefs and be re-registered via <c>[DidReloadScripts]</c>.</description></item>
    /// <item><description><c>Thread.Sleep</c> in executeMethod deadlocks — the compiler
    /// shares the main thread.</description></item>
    /// </list>
    /// </remarks>
    private static void ContinuePendingBuild()
    {
        if (!EditorPrefs.GetBool(PendingBuildActiveKey, false))
        {
            EditorApplication.update -= ContinuePendingBuild;
            return;
        }

        var startTicks = long.Parse(EditorPrefs.GetString(PendingBuildStartTicksKey, "0"));
        var startTime = startTicks > 0 ? new DateTime(startTicks, DateTimeKind.Utc) : DateTime.UtcNow;
        var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;

        var platform = EditorPrefs.GetString(PendingBuildPlatformKey);

        if (elapsed > ScriptCompilationTimeoutSeconds)
        {
            ClearPendingBuildState();
            EditorApplication.update -= ContinuePendingBuild;
            Debug.LogError(
                $"[CytoidCoreBuild] Timed out after {ScriptCompilationTimeoutSeconds}s "
                + $"waiting for script compilation ({platform} plugin export).");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }

        if (IsScriptCompilationPending()) return;

        // Post-switch race guard: SwitchActiveBuildTarget returns before isCompiling
        // flips true. Wait the grace window before treating "not pending" as "done".
        if (elapsed < ScriptCompilationGracePeriodSeconds) return;

        var outputDir = EditorPrefs.GetString(PendingBuildOutputDirKey);
        var packageFramework = EditorPrefs.GetBool(PendingBuildPackageKey);
        var iosSdk = EditorPrefs.GetString(PendingBuildIosSdkKey);

        ClearPendingBuildState();
        EditorApplication.update -= ContinuePendingBuild;

        Debug.Log($"[CytoidCoreBuild] Script compilation finished ({platform} plugin export).");

        try
        {
            if (platform == "android")
            {
                RunAndroidExport(
                    PluginBuildScenes,
                    outputDir,
                    FlutterHostLibraryApplicationIdentifier,
                    builtPath =>
                    {
                        Debug.Log(
                            $"[CytoidCoreBuild] Android export at {builtPath}\n"
                            + $"  Define: {FlutterHostDefineSymbol}\n"
                            + $"  Library applicationId: {FlutterHostLibraryApplicationIdentifier}\n"
                            + $"  Flutter plugin package: {FlutterHostApplicationId}\n"
                            + "  JNI callback: org.cytoid.gamecore.UnityHostCallback.onMessage");
                        PackageAndroidLibraryForFlutter();
                    });
            }
            else
            {
                RunIOSExport(
                    PluginBuildScenes,
                    outputDir,
                    FlutterHostLibraryApplicationIdentifier,
                    iosSdk,
                    builtPath =>
                    {
                        Debug.Log(
                            $"[CytoidCoreBuild] iOS export at {builtPath}\n"
                            + $"  Define: {FlutterHostDefineSymbol}\n"
                            + $"  iOS SDK: {iosSdk} ({ResolveUnityIosSdkVersion(iosSdk)})\n"
                            + $"  Library bundle id: {FlutterHostLibraryApplicationIdentifier}\n"
                            + $"  Flutter plugin package: {FlutterHostApplicationId}\n"
                            + "  Native callback: CytoidHostNative_SetMessageHandler from Flutter host");
                        if (packageFramework)
                        {
                            PackageIOSLibraryForFlutter();
                        }
                        else
                        {
                            Debug.Log(
                                "[CytoidCoreBuild] Skipped iOS UnityFramework packaging. "
                                + "Run flutter_plugin/tool/build_unity_ios_framework.sh on macOS.");
                        }
                    });
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CytoidCoreBuild] Export failed: {ex}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }

        // Without -quit (which we now require), Unity stays open after the export.
        // Exit explicitly so batchmode CI runs terminate.
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }

    private static void SetPendingBuildState(string platform, string outputDir, bool packageFramework, string iosSdk)
    {
        EditorPrefs.SetBool(PendingBuildActiveKey, true);
        EditorPrefs.SetString(PendingBuildPlatformKey, platform);
        EditorPrefs.SetString(PendingBuildOutputDirKey, outputDir);
        EditorPrefs.SetBool(PendingBuildPackageKey, packageFramework);
        EditorPrefs.SetString(PendingBuildIosSdkKey, iosSdk);
        EditorPrefs.SetString(PendingBuildStartTicksKey, DateTime.UtcNow.Ticks.ToString());
    }

    private static void ClearPendingBuildState()
    {
        EditorPrefs.DeleteKey(PendingBuildActiveKey);
        EditorPrefs.DeleteKey(PendingBuildPlatformKey);
        EditorPrefs.DeleteKey(PendingBuildOutputDirKey);
        EditorPrefs.DeleteKey(PendingBuildPackageKey);
        EditorPrefs.DeleteKey(PendingBuildIosSdkKey);
        EditorPrefs.DeleteKey(PendingBuildStartTicksKey);
    }

    private static void LogBuildReportErrors(BuildReport report, string platformLabel)
    {
        if (IsScriptCompilationPending())
        {
            Debug.LogError(
                $"[CytoidCoreBuild] {platformLabel} build aborted because scripts were still compiling. "
                + "Retry after the Editor finishes recompiling.");
        }

        foreach (var step in report.steps)
        {
            foreach (var message in step.messages)
            {
                if (message.type != LogType.Error && message.type != LogType.Exception)
                {
                    continue;
                }

                Debug.LogError($"[CytoidCoreBuild][{platformLabel}] {message.content}");
            }
        }

        Debug.LogError(
            $"[CytoidCoreBuild] {platformLabel} build summary: "
            + $"result={report.summary.result}, "
            + $"errors={report.summary.totalErrors}, "
            + $"warnings={report.summary.totalWarnings}, "
            + $"output={report.summary.outputPath}");
    }

    private static void PackageAndroidLibraryForFlutter()
    {
        RunFlutterPluginToolScript(BuildUnityAarScript, "Android AAR packaging");
        var artifactsPath = ResolvePathUnderProjectRoot(FlutterAndroidArtifactsRelativePath);
        Debug.Log(
            $"[CytoidCoreBuild] Android plugin artifacts ready at {artifactsPath}\n"
            + "  cytoid-unity-core.aar and dependency AARs");
    }

    private static void PackageIOSLibraryForFlutter()
    {
        RunFlutterPluginToolScript(BuildUnityIosFrameworkScript, "iOS UnityFramework packaging");
        var artifactsPath = ResolvePathUnderProjectRoot(FlutterIOSArtifactsRelativePath);
        Debug.Log(
            $"[CytoidCoreBuild] iOS plugin artifacts ready at {artifactsPath}\n"
            + "  UnityFramework.framework and UnityFramework.xcframework");
    }

    private static void RunFlutterPluginToolScript(string scriptFileName, string logLabel)
    {
        var projectRoot = Path.GetDirectoryName(Application.dataPath) ?? ".";
        var scriptPath = Path.GetFullPath(
            Path.Combine(projectRoot, FlutterPluginToolRelativeDir, scriptFileName));
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"Flutter plugin tool script not found: {scriptPath}");
        }

        Debug.Log($"[CytoidCoreBuild] Running {logLabel}: {scriptPath}");

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = ResolveBashExecutable(),
            Arguments = $"\"{scriptPath}\"",
            WorkingDirectory = projectRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(psi);
        if (process == null)
        {
            throw new Exception($"Failed to start {logLabel}.");
        }

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        LogProcessOutput(logLabel, stdout, LogType.Log);
        if (process.ExitCode != 0)
        {
            LogProcessOutput(logLabel, stderr, LogType.Error);
            throw new Exception(
                $"{logLabel} failed with exit code {process.ExitCode}. See Console for details.");
        }

        LogProcessOutput(logLabel, stderr, LogType.Warning);
    }

    private static void LogProcessOutput(string logLabel, string text, LogType logType)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        foreach (var line in text.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var message = $"[CytoidCoreBuild][{logLabel}] {line.TrimEnd()}";
            switch (logType)
            {
                case LogType.Error:
                    Debug.LogError(message);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(message);
                    break;
                default:
                    Debug.Log(message);
                    break;
            }
        }
    }

    private static string ResolveBashExecutable()
    {
#if UNITY_EDITOR_WIN
        return "bash";
#else
        return "/bin/bash";
#endif
    }

    private static string ResolvePathUnderProjectRoot(string relativePath)
    {
        var unityProjectRoot = Path.GetDirectoryName(Application.dataPath) ?? ".";
        return Path.GetFullPath(Path.Combine(unityProjectRoot, relativePath));
    }

    private static string ResolveFlutterUnityLibraryOutputPath()
    {
        var unityProjectRoot = Path.GetDirectoryName(Application.dataPath) ?? ".";
        var relativePath = EditorPrefs.GetString(
            FlutterUnityLibraryPathPrefKey,
            DefaultFlutterUnityLibraryRelativePath);
        return Path.GetFullPath(Path.Combine(unityProjectRoot, relativePath));
    }

    private static string ResolveFlutterUnityIOSOutputPath()
    {
        var unityProjectRoot = Path.GetDirectoryName(Application.dataPath) ?? ".";
        var relativePath = EditorPrefs.GetString(
            FlutterUnityIOSPathPrefKey,
            DefaultFlutterUnityIOSRelativePath);
        return Path.GetFullPath(Path.Combine(unityProjectRoot, relativePath));
    }

    private static string MergeDefineSymbols(string current, string[] extraDefineSymbols)
    {
        var symbols = current
            .Split(';')
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .ToList();

        foreach (var defineSymbol in extraDefineSymbols)
        {
            if (!symbols.Contains(defineSymbol))
            {
                symbols.Add(defineSymbol);
            }
        }

        return string.Join(";", symbols);
    }
}
