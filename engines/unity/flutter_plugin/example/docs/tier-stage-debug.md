# Tier stage debug (example app)

The **Tier** tab launches a single `bridge.play.start` with manual `tierPlay` fields. Use it to simulate cross-stage state without a full tier run UI.

## Fields

| Field | Purpose |
|-------|---------|
| `initialHealth` | Simulates HP carried from a previous stage |
| `initialCombo` | Simulates combo carried from a previous stage |
| `stageIndex` / `tierId` | Echoed in `game.play.result.tierPlay` |
| `introLabel` | Optional tier intro splash in Unity |

## Mock engine

Without a Unity AAR, the mock runtime returns a synthetic `game.play.result` with `tierPlay.finalHealth ≈ initialHealth × 0.85` and `endingCombo = initialCombo + 50`.

## Next stage manually

After a run, note `tierPlay.finalHealth` and `endingCombo` on the result screen, then enter them as the next `initialHealth` / `initialCombo` and increment `stageIndex`.
