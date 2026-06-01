# Gameplay Engine

## Game Lifecycle (Game.cs)

```
Initialize() → StartGame() → Update() loop → Complete()/Fail() → Dispose()
```

### Initialization Sequence

1. Determine `GameMode` from `Context.SelectedGameMode` (Standard/Practice/Calibration/Tier/GlobalCalibration)
2. Load chart JSON from `Level.Path + chartMeta.path`, parse into `Chart` object
3. Load audio via `AudioClipLoader`, register with `AudioManager` as "Level"
4. Load optional storyboard from `storyboard.json`
5. Create `GameState` (scoring state) and `GameConfig` (visual config)
6. Pre-allocate `ObjectPool` based on chart note density
7. Call `StartGame()` which plays music via DSP scheduling

### Update Loop (per frame)

1. `SynchronizeMusic()` — syncs `Game.Time` to `AudioSettings.dspTime` (resyncs every 600 frames + first 0.5s)
2. Process speed change events from `event_order_list`
3. Advance `CurrentPageId` as scan line passes page boundaries (fires boundary events)
4. Spawn notes: `while (note.intro_time - 1f < Time) ObjectPool.SpawnNote(note)` — notes spawn ~1 second before their visible intro time
5. Invoke `onGameUpdate` / `onGameLateUpdate` for all registered listeners

## Note Type Hierarchy

```
Note (abstract base)
├── ClickNote        — Simple tap
├── HoldNote         — Press and hold (duration-based grading)
│   └── LongHoldNote — Extended hold
├── FlickNote        — Swipe horizontally after touch-down
├── DragHeadNote     — Chain head, interpolates between linked positions
└── DragChildNote    — Chain segment, slides through on touch
```

`NoteType` enum: `Click/Hold/LongHold/DragHead/DragChild/Flick/CDragHead/CDragChild`

Each note type has its own `CalculateGrade()` with type-specific judgment windows.

### Note Base Class Lifecycle

```
Initialize → SetData → Clear → Collect
CalculateGrade / TryClear (judgment)
Auto-play support
Miss detection (timeout-based)
Position update (per frame)
```

## Judgment System

### Ranked Mode Judgment Windows (from Note.CalculateGrade())

| Grade | Early Window | Late Window | Score Weight | Accuracy Weight |
|-------|-------------|-------------|-------------|----------------|
| Perfect | ≤ 0.040s | ≤ 0.040s | 1.000 | 1.0 |
| Great | 0.040–0.070s | 0.040–0.070s | 0.900 | 0.7 |
| Good | 0.070–0.200s | 0.070–0.150s | 0.500 | 0.3 |
| Bad | 0.200–0.400s | 0.150–0.200s | 0.100 | 0.0 |
| Miss | > 0.400s or timeout (> 0.3s) | > 0.200s or timeout | 0.000 | 0.0 |

"Great" grade has a continuous `GreatGradeWeight` (0 to 1) that interpolates the score between Great and Perfect.

### Type-Specific Judgment Variations

- **Hold notes**: Graded by hold duration percentage — Perfect (>95%), Great (>70%), Good (>50%), Bad (>30%), Miss.
- **Flick notes**: Tighter windows — Perfect ≤ 0.060s, Great ≤ 0.150s.
- **Drag notes**: Very generous — Perfect if touched within 0.5s early or 0.2s late.
- **Practice mode**: Wider windows — Perfect ≤ 0.070s, Great ≤ 0.200s, Good ≤ 0.400s, Bad ≤ 0.800s.

## Scoring System

### Score Grades

| Grade | Threshold |
|-------|-----------|
| MAX | 1,000,000 |
| SSS | ≥ 999,000 |
| SS | ≥ 995,000 |
| S | ≥ 990,000 |
| AA | ≥ 950,000 |
| A | ≥ 900,000 |
| B | ≥ 800,000 |
| C | ≥ 700,000 |
| D | ≥ 600,000 |
| F | < 600,000 |

### Score Calculation

```
maxNoteScore = 1,000,000 / NoteCount
noteScore = maxNoteScore * grade.GetScoreWeight(ranked: true)
// Great grade interpolates: weight between 0.9 and 1.0 based on GreatGradeWeight
noteScore *= NoteScoreMultiplier
Score += noteScore
```

### NoteScoreMultiplier Evolution

- Perfect: +0.004 × sqrt(NoteCount) / 3
- Great: +0.002 × factor
- Good: +0.001 × factor
- Bad: −0.025 × factor
- Miss: −0.05 × factor
- Clamped to [0, 1]

### Other Metrics

- **Accuracy**: `accumulatedAccuracy / ClearCount`, using accuracy weights.
- **Combo**: Increments on non-Bad/non-Miss, resets to 0 on Bad/Miss.
- **Health** (Hard/ExHard/Tier mode): HP changes per note type and grade; game fails when HP ≤ 0.

## Chart Data Format

Charts are JSON (with legacy text format fallback). The `ChartModel` structure:

```json
{
  "time_base": 480,
  "tempo_list": [{"tick": 0, "value": 500000}],
  "page_list": [{"start_tick": 0, "end_tick": 480, "scan_line_direction": 1}],
  "note_list": [{"id": 0, "type": 0, "tick": 240, "x": 0.5, "page_index": 0, "hold_tick": 0, "next_id": -1}],
  "event_order_list": [],
  "music_offset": 0.0,
  "size": 1.0,
  "opacity": 1.0,
  "ring_color": null,
  "fill_colors": []
}
```

### Tick-to-Time Conversion

Traverses `tempo_list` to handle tempo changes: `result += tickDelta * 1e-6 * tempoValue / timeBase`.

### Pages & Scan Line

Pages define the scan line sweep direction and duration. The scan line bounces between boundaries (top/bottom). Note positions are calculated from chart X coordinate and page-relative tick position, mapped to screen space.

## Audio-Timing Synchronization

`SynchronizeMusic()` in `Game.cs`:

- Primary sync: `AudioSettings.dspTime` (high-precision audio clock) resynced every ~10 seconds
- Fallback: `Time.unscaledDeltaTime` accumulation between syncs
- Chart time = `dspTime - ChartOffset + MusicOffset - MusicStartedTimestamp`
- Music is played via `AudioManager.Controller.PlayScheduled()` using Unity's DSP scheduling

## Key Files

| File | Purpose |
|------|---------|
| `Game/Game.cs` | Main game controller — lifecycle, music sync, note spawning, events |
| `Game/GameState.cs` (~764 lines) | Scoring state machine — judgment, combo, health, accuracy |
| `Game/GameConfig.cs` | Visual/audio configuration — note sizes, hitboxes, colors, chart offset |
| `Game/PlayerGame.cs` | Standalone/debug variant — allows seeking, live storyboard reload |
| `Game/InputController.cs` | Touch input routing — finger down/update/up mapped to note types (uses LeanTouch) |
| `Game/ObjectPool.cs` | Generic pool for notes, drag lines, particle effects |
| `Game/EffectController.cs` | Visual effects — clear rings, miss particles, hold particles, ripple effects |
| `Game/Mod.cs` | Mod enum — FlipX/Y/All, Slow/Fast, FC/AP, Hard/ExHard, HideScanline/HideNotes, AutoDrag/AutoHold/AutoFlick/Auto |
| `Game/NoteGrade.cs` | NoteGrade enum with score weights and accuracy weights |
| `Game/ScoreGrade.cs` | ScoreGrade enum with score thresholds |
| `Game/Chart/ChartModel.cs` | Chart JSON data model — tempo_list, page_list, note_list, event_order_list |
| `Game/Chart/Chart.cs` | Chart parser — tick-to-time, note position, scanner, legacy converter, approach rate |
| `Game/Notes/NoteType.cs` | NoteType enum with miss thresholds |
| `Game/Notes/Note.cs` | Abstract note base — lifecycle, judgment, auto-play, miss detection |
| `Game/Notes/ClickNote.cs` | Click note implementation |
| `Game/Notes/HoldNote.cs` | Hold note with finger tracking and hold progress |
| `Game/Notes/LongHoldNote.cs` | Extended hold note |
| `Game/Notes/FlickNote.cs` | Swipe note with tighter judgment |
| `Game/Notes/DragHeadNote.cs` | Drag chain head with generous timing |
| `Game/Notes/DragChildNote.cs` | Drag chain child note |
| `Game/Notes/DragLineElement.cs` | Visual connector line between drag notes |
| `Game/Notes/NoteRenderer.cs` | Abstract renderer base with collision detection |
| `Game/Notes/GameRenderer.cs` | Top-level game renderer — boundaries, cover, scanner speed |
| `Game/Notes/Classic/` | Classic-style renderers for each note type (8 files) |
| `Game/GameObjectProvider.cs` | Singleton providing prefab references for all note types |

## Navigation-to-Game Interface

### Starting a Game (from GamePreparationScreen.OnStartButton())

1. Set `Context.SelectedGameMode` (Standard/Practice/Calibration)
2. Set `Context.SelectedMods` from player settings
3. `new SceneLoader("Game").Load()` — loads the Game Unity scene asynchronously
4. The `Game` scene's `Game.cs` reads from `Context.SelectedLevel`, `Context.SelectedDifficulty`, `Context.SelectedGameMode`, `Context.SelectedMods`
5. On completion: `Game.Complete()` → saves to `LevelRecord` → loads "Navigation" scene → shows `ResultScreen`

### Context Fields Used as the Interface

| Direction | Field | Type |
|-----------|-------|------|
| Flutter → Unity | `SelectedLevel` | Level object |
| Flutter → Unity | `SelectedDifficulty` | Easy/Hard/Extreme |
| Flutter → Unity | `SelectedGameMode` | GameMode enum |
| Flutter → Unity | `SelectedMods` | HashSet\<Mod\> |
| Unity → Flutter | `GameState` | Score, accuracy, grade counts, max combo |
| Unity → Flutter | `GameErrorState` | Error info if loading fails |
