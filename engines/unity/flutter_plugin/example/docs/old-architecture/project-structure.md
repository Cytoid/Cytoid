# Project Structure & Boot Sequence

## Unity Scenes (4 scenes)

| Scene | File | Purpose |
|-------|------|---------|
| **Bootstrapper** | `Assets/Scenes/Bootstrapper.unity` | First scene loaded on app launch. Contains `Bootstrapper` MonoBehaviour and `Context` singleton. Shows a splash screen (TapTap distribution only), then immediately loads the Navigation scene. |
| **Navigation** | `Assets/Scenes/Navigation.unity` | Main menu/UI scene. Houses all navigation screens, `ScreenManager`, `AudioManager`, `NavigationBackdrop`, and character display. This is where the user spends all non-gameplay time. |
| **Game** | `Assets/Scenes/Game.unity` | Gameplay scene. Contains `Game` (or `PlayerGame`) MonoBehaviour, camera, note rendering area, input controller, effect controller, and game HUD overlays (score, combo, accuracy, pause/failed screens). |
| **Player** | `Assets/Scenes/Player.unity` | Audio/video playback scene (for video backgrounds or preview players). |

## Entry Point & Boot Sequence

1. Unity loads `Bootstrapper.unity` (Build Scene 0)
2. `Bootstrapper.Awake()` runs → optionally shows TapTap splash → calls `SceneManager.LoadScene("Navigation")`
3. `Context.Awake()` runs (DontDestroyOnLoad singleton) → triggers `InitializeApplication()`:
   - Android storage path setup
   - `Player.Initialize()` (in-memory default settings; Flutter host applies `GameLaunchSettings` later)
   - `AudioManager.Initialize()` (native audio)
   - `FontManager.LoadFonts()`
   - Localization setup
   - `BundleManager.Initialize()` (catalog + built-in bundles)
   - Scene-dependent init:
     - Navigation scene → `CharacterManager` → `ScreenManager` → `InitializationScreen`
     - Game scene → `Game.Initialize()` waits for `Context.IsInitialized`
4. Navigation scene loads, `InitializationScreen` is shown ("Touch to Start")

## Two-Scene Architecture

The app uses exactly **2 active scenes** that swap back and forth:

1. **Navigation** (menus) — contains `ScreenManager`, `AudioManager`, all UI screens
2. **Game** (gameplay) — contains `Game`/`PlayerGame`, note rendering, input handling

Scene transitions are managed by `SceneLoader` which fires `PreSceneChanged` / `PostSceneChanged` events. `Context` listens to these events to save/restore navigation history, unload/load character assets, and determine which screen to show after returning from gameplay.

## Module Map: `Assets/Scripts/` Directories

### Core Application Modules

- **`Game/`** (~78 files) — Gameplay engine. Core game loop, note spawning, chart processing, scoring, rendering.
- **`Navigation/`** (~100+ files) — All menu/UI screens and reusable UI components.
- **`Screen/`** (5 files) — Screen management framework. Custom screen lifecycle system.
- **`Level/`** (4 files) — Level data models.
- **`Online/`** (~30 files) — API data models (REST API response types).
- **`Player/`** (4 files) — Player data (local settings, auth, local level index).
- **`Character/`** (5 files) — Character system (asset bundles, meta, variants).
- **`Storyboard/`** (~53 files) — Custom storyboard engine for chart backgrounds.
- **`Secure/`** (13 files) — Anti-cheat / obfuscation.
- **`Utils/`** (~50 files) — Shared utilities (singletons, bootstrapper, scene loader, extensions).

### Root-Level Singletons

| File | Purpose |
|------|---------|
| `Context.cs` | God class / service locator. Holds all global state and services. |
| `LevelManager.cs` | Level install/load/download manager. |
| `AudioManager.cs` | Audio playback (Unity + native audio). |
| `BundleManager.cs` | AssetBundle catalog & download system. |
| `FontManager.cs` | Font loading. |
| `ConsoleManager.cs` | Developer console. |
| `BuiltInData.cs` | Constants for built-in levels, default character, etc. |
| `InitializationState.cs` | First-launch flow state. |
| `SentryOptionConfiguration.cs` | Error reporting config. |

## Service Locator: `Context`

There is **no dependency injection framework** (no Zenject, no VContainer). The architecture uses:

- **`Context` as a static service locator** — All services are static fields on `Context` (e.g., `Context.AudioManager`, `Context.ScreenManager`, `Context.LevelManager`)
- **`SingletonMonoBehavior<T>`** — Unity MonoBehaviour singletons for components that need to live in a scene (`AudioManager`, `ScreenManager`, `Context`)
- **Plain C# singletons** instantiated in `Context` (`LevelManager`, `CharacterManager`, `BundleManager`, `FontManager`, `Player`, `OnlinePlayer` are `new`'d as static fields)
- **UnityEvents** for loose coupling (`PreSceneChanged`, `OnApplicationInitialized`, `OnLanguageChanged`, etc.)

### Context Static Fields

```
Context (SingletonMonoBehavior, DontDestroyOnLoad)
├── AudioManager        — sound system
├── ScreenManager       — navigation screen stack
├── LevelManager        — level loading/installing
├── CharacterManager    — character asset bundles
├── BundleManager       — AssetBundle catalog & downloading
├── FontManager         — font loading
├── AssetMemory         — texture/asset caching
├── Player              — local player settings & data
├── OnlinePlayer        — authentication & online profile
├── Library             — local level library index
├── (no local DB)       — persistence moved to Flutter host in cytoid-core-unity
├── GameState           — current game state
├── TierState           — tier/ranked mode state
├── InitializationState — first-launch flow state
├── SelectedLevel       — selected level for gameplay
├── SelectedDifficulty  — selected difficulty
├── SelectedGameMode    — current game mode
└── SelectedMods        — active modifiers
```
