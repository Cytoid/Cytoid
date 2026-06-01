# Navigation & UI Architecture

## Screen Framework

### Core Classes

| Class | File | Purpose |
|-------|------|---------|
| `Screen` | `Assets/Scripts/Screen/Screen.cs` | Abstract base class for all screens. Defines lifecycle with payload loading pattern. |
| `ScreenManager` | `Assets/Scripts/Screen/ScreenManager.cs` | Singleton. Manages screen creation, destruction, history stack, and animated transitions. |
| `ScreenHandler` | `Assets/Scripts/Screen/ScreenHandler.cs` | Listener interfaces (`ScreenInitializedListener`, `ScreenBecameActiveListener`, etc.). |
| `ScreenTransition` | `Assets/Scripts/Screen/ScreenTransition.cs` | Enum: `In, Out, Left, Right, Up, Down, Fade, None`. |
| `Transition` | `Assets/Scripts/Scripts/Screen/Transition.cs` | Element-level transition enum: `Top, Bottom, Left, Right, Up, Down, Default`. |

### Screen Lifecycle

```
Destroyed → Initialized → Active → Inactive → Destroyed
                         │
              LoadPayload → Render → OnRendered
              (async)      (sync)   (post-render)
```

- `ScreenPayload` arrives as intent data (typed per screen, e.g., `GamePreparationScreen.Payload { Level }`)
- `LoadPayload(ScreenLoadPromise)` — async data loading (API calls, etc.)
- `Render()` — synchronous UI population
- `OnRendered()` — post-render setup (animations, scroll position restore)

### Navigation Pattern

`ScreenManager` is the central controller. It manages a `Stack<Intent>` (history) and handles screen creation/instantiation from prefabs.

- `Screen.ChangeScreen(targetId, transition)` is the one API for all navigation.
- `Intent = (ScreenId, ScreenPayload)` — carries data to the target screen.
- History management: Screens are pushed onto a stack. Back navigation uses `PopAndPeekHistory()`. Long-press on `NavigationElement` clears history and jumps to MainMenu.
- Screens are loaded lazily (instantiated from prefabs on first visit) and optionally destroyed when leaving.

### Transitions

8 transition types via `ScreenTransition` enum: `In, Out, Left, Right, Up, Down, Fade, None`. Transitions are implemented with **DOTween** (fade + scale/position animations). Duration is configurable per navigation call (~0.4s default).

## All Screens (18 screens)

| Screen | Directory | Purpose |
|--------|-----------|---------|
| `InitializationScreen` | `Screens/Initialization/` | App startup: loads levels, connects to server, "Touch to Start" |
| `MainMenuScreen` | `Screens/MainMenu/` | Main menu hub: Free Play, Community, Events, Settings, Profile links |
| `LevelSelectionScreen` | `Screens/LevelSelection/` | Local level browser with sorting, filtering, search, batch delete |
| `GamePreparationScreen` | `Screens/GamePreparation/` | Level detail/prepare screen: cover, preview audio, difficulty select, rankings, rating, settings, start game |
| `ResultScreen` | `Screens/Result/` | Post-gameplay results: score, accuracy, combo, grade, rankings, upload, share, retry |
| `SettingsScreen` | `Screens/Settings/` | Settings with 4 tabs: General, Gameplay, Visual, Advanced |
| `ProfileScreen` | `Screens/Profile/` | Player profile: stats, character display, leaderboard, sign out, offline toggle |
| `SignInScreen` | `Screens/SignIn/` | Sign in / sign up with credentials |
| `CommunityHomeScreen` | `Screens/CommunityHome/` | Community landing: featured collections, new/trending levels, search |
| `CommunityLevelSelectionScreen` | `Screens/CommunityLevelSelection/` | Online level browser with filters, sorting, batch download |
| `CommunityCollectionSelectionScreen` | `Screens/CommunityCollectionSelection/` | Online collection browser |
| `CollectionDetailsScreen` | `Screens/CollectionDetails/` | Collection detail: levels list, batch download |
| `CharacterSelectionScreen` | `Screens/CharacterSelection/` | Character picker with variants, levels, EXP |
| `EventSelectionScreen` | `Screens/EventSelection/` | Event browser with objectives, tier mode entry |
| `TierSelectionScreen` | `Screens/TierSelection/` | Tier mode: select tier stages, view rewards |
| `TierBreakScreen` | `Screens/TierBreak/` | Inter-stage results in tier mode |
| `TierResultScreen` | `Screens/TierResult/` | Final tier mode results |
| `TrainingSelectionScreen` | `Screens/TrainingSelection/` | Training mode level selection |

## Navigation Flow Map

```
Initialization → MainMenu
  ├── LevelSelection → GamePreparation → [Game Scene] → Result
  ├── CommunityHome → CommunityLevelSelection → GamePreparation
  │                 → CommunityCollectionSelection → CollectionDetails → GamePreparation
  ├── Settings
  ├── Profile (with tabs: profile, leaderboard, records)
  ├── SignIn
  ├── CharacterSelection
  ├── EventSelection → TierSelection → [Game Scene] → TierBreak → TierResult
  └── TrainingSelection
```

## Key UI Components / Elements

| Component | File | Purpose |
|-----------|------|---------|
| `NavigationElement` | `Navigation/Elements/NavigationElement.cs` | Navigation button: navigates to target screen or back. Supports long-press to go home. |
| `TransitionElement` | `Navigation/Elements/TransitionElement.cs` | Animated show/hide for UI sub-elements (enter/leave with DOTween). |
| `Dialog` | `Navigation/Elements/Dialog.cs` | Modal dialog with positive/negative buttons, progress bar. |
| `Toast` | `Navigation/Elements/Toast.cs` | Toast notification queue with success/failure/spinner icons. |
| `LevelCard` | `Navigation/Elements/LevelCard.cs` | Reusable level card component (cover, title, difficulty, click handler). |
| `ProfileWidget` | `Navigation/Elements/ProfileWidget.cs` | Persistent top-right profile avatar widget across all screens. |
| `SpinnerOverlay` | `Navigation/Elements/SpinnerOverlay.cs` | Full-screen loading spinner overlay. |
| `SettingsFactory` | `Navigation/Elements/SettingsFactory.cs` | Factory for creating settings UI elements dynamically. |
| `DialogObjectProvider` | `Navigation/DialogObjectProvider.cs` | Singleton prefab provider for Dialog instances. |
| `RateLevelDialog` | `Navigation/Elements/RateLevelDialog.cs` | Level rating dialog. |
| `RatingTab` | `Navigation/Elements/RatingTab.cs` | Rating display tab. |
| `LevelBatchSelectionDownloadHandler` | `Screens/CommunityLevelSelection/` | Batch level download handler. |

## Global Overlays

Several UI elements live outside the screen system and persist across screens:

- **ProfileWidget** — Persistent avatar in top-right corner, visible on most screens
- **NavigationBackdrop** — Blurred background behind screens
- **SpinnerOverlay** — Full-screen loading overlay
- **Toast** — Queued notification banners
- **Dialog** — Modal alerts/prompts (instantiated from prefab via `DialogObjectProvider`)

## Deep Links

`NavigationBehavior` (singleton) handles `cytoid://levels/{id}` deep links, file imports, offline mode toggling, and app-resume level installation.

## UI Framework Stack

- **Unity UGUI** (UnityEngine.UI) — Canvas, CanvasGroup, ScrollRect, LayoutGroup
- **DOTween** — All animations (transitions, fades, movement, scaling)
- **UniTask** — Async/await for Unity (replaces coroutines)
- **ProceduralImage** — Custom UI image component
- **LoopVerticalScrollRect** — Recycling scroll list for performance
