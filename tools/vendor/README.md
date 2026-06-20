# Vendor bundles (maintainers)

Paid Unity payloads live under **`engines/unity/Assets/Vendor/`** (gitignored), matching other Unity `Assets/` folders (`Scripts`, `Packages`, ...).

## Layout

```
engines/unity/Assets/Vendor/      # gitignored
└── StoryboardFilters/            # Camera Filter Pack + Sleek Render + Vendor*.cs
    └── (future packages as siblings, PascalCase folder names)
```

## Pack

```bash
bash tools/vendor/pack.sh
```

Output: `Builds/vendor-bundles/cytoid-core-unity-vendor-YYYYMMDD.zip`

## Clean (remove local install)

```bash
bash tools/vendor/clean.sh
```

Deletes `engines/unity/Assets/Vendor/` and stray `Vendor.meta`. Use before switching to fallback-only, or to reinstall from a fresh zip.

## Install

From the Unity project root:

```bash
cd engines/unity
unzip -o /path/to/cytoid-core-unity-vendor-YYYYMMDD.zip
```

See [docs/vendor.md](../../docs/vendor.md).
