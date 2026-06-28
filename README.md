<img src="https://i.imgur.com/vc3ylH1.jpg" style="width: 100%">

# Cytoid [![CodeFactor](https://www.codefactor.io/repository/github/cytoid/cytoid/badge)](https://www.codefactor.io/repository/github/cytoid/cytoid) [![Join the Discord](https://discordapp.com/api/guilds/362884768498712579/widget.png?style=shield)](https://discord.gg/cytoid)

A community-driven touchscreen music game available on [App Store](https://itunes.apple.com/us/app/cytoid/id1266582726) and [Google Play](https://play.google.com/store/apps/details?id=me.tigerhix.cytoid).

## Getting Started

### Gameplay Core

The Unity gameplay core lives at `engines/unity/` and is built with [Unity 6000.0.75f1 (Personal)](https://unity3d.com).

### Flutter Integration

The Unity core is embedded via the `cytoid_game_core` Flutter plugin at `engines/unity/flutter_plugin/`. The plugin provides an engine-agnostic JSON host protocol.

**Runtime presentation:**
- **Android:** Exclusive Unity Activity (not a platform view fragment)
- **iOS:** Exclusive Unity window (not a platform view)

See `docs/host-protocol-v2.md` for the current protocol specification.

### Unity Assets

Licensed Unity packages are optional: unzip a maintainer **`Assets/Vendor/`** bundle at `engines/unity/`. See [docs/vendor.md](docs/vendor.md). Without it, in-repo fallbacks (e.g. storyboard shaders) are used.

You also need to install Native Audio (free since 2025-09-22) from the Asset Store.

## License

Source code (excluding graphical assets, unless stated otherwise) is distributed under [GPL-3.0 License](https://www.gnu.org/licenses/gpl-3.0.en.html).
