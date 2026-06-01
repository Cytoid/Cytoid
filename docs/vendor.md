# Vendor Assets

`engines/unity/Assets/Vendor/` is intentionally gitignored. It may contain paid or otherwise
non-redistributable Unity packages used by maintainer builds.

The storyboard vendor bundle is expected at:

```text
engines/unity/Assets/Vendor/StoryboardFilters/
```

Maintainer CI can install it from a private zip before Unity import:

```sh
export CYTOID_VENDOR_ARCHIVE=https://example.com/private/storyboard-filters.zip
export CYTOID_VENDOR_ARCHIVE_SHA256=<sha256>
engines/unity/flutter_plugin/tool/install_vendor_from_archive.sh
```

Accepted zip layouts:

```text
Assets/Vendor/StoryboardFilters/...
Vendor/StoryboardFilters/...
StoryboardFilters/...
```

If no archive is configured, builds continue with the in-repo fallback storyboard
effects in `engines/unity/Assets/Shaders/Storyboard/` and `engines/unity/Assets/Scripts/Storyboard/PostProcess/`.
Do not commit `engines/unity/Assets/Vendor/` contents or generated Unity export artifacts.
