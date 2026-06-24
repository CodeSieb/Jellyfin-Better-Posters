# Better Poster — Minimal (Jellyfin)

A stripped-down Jellyfin plugin repo that surfaces [btttr.cc](https://btttr.cc) posters as primary images for Movies and TV Series.

This is a **manifest plugin repository** — point Jellyfin at the raw `manifest.json` URL below so users can install the plugin from Dashboard → Plugins → Catalog.

> Repository URL (what Jellyfin users add):
> `https://raw.githubusercontent.com/TheAceOfficials/BetterPoster-Jellyfin-Minimal/main/manifest.json`

## What this plugin does

- Acts as an Official Remote Image Provider for `Movie` and `Series` items.
- Tries IMDb first; falls back to TMDB when IMDb is missing and the toggle is on.
- Respects per-type toggle(s) so you can opt out of Movies and/or Series independently.
- Scheduled refresh task (default every 24h, configurable from Dashboard → Scheduled Tasks).
- Settings page with overlay toggles, language, preview button (renders a sample POSTER URL inline) and Reset to Defaults.

## What this plugin does NOT do

This is the minimal fork of the parent BetterPoster plugin — Trakt token integration and explicit TMDB-aware per-item behaviour are intentionally absent in favour of keeping the surface small.

## Repo layout

```
├── manifest.json            # Outer catalog manifest (this repo)
├── README.md                # This file
├── .gitignore               # Standard dotnet/git ignores
├── build_plugin_zip.py      # Local build script: DLL -> release zip + MD5
├── Plugin.cs
├── BtttrImageProvider.cs
├── BtttrPosterUrlBuilder.cs
├── BetterPostersRefreshTask.cs
├── PluginServiceRegistrator.cs
├── BetterPosterMinimal.csproj
└── Configuration/
    ├── PluginConfiguration.cs
    └── configPage.html
```

## Build & release

The DLL produced by `dotnet build -c Release` lives at:

```
bin/Release/net9.0/Jellyfin.Plugin.BetterPosterMinimal.dll
```

Packaging for distribution is done locally by `build_plugin_zip.py`, which:

1. Generates an inner `meta.json` (mirrors the per-version fields of the catalog manifest).
2. Zips that with the DLL into `dist/Jellyfin.Plugin.BetterPosterMinimal-1.0.0.0.zip`.
3. Writes `<zip>.md5` next to it.

For Jellyfin the checksum in `manifest.json` is the **uppercase MD5 of the file**. Run:

```bash
python build_plugin_zip.py
# On Windows, certutil -hashfile dist\Jellyfin.Plugin.BetterPosterMinimal-1.0.0.0.zip MD5
```

then paste the uppercase MD5 into `manifest.json > versions[0].checksum` and remember to keep them in sync when you cut a new release.

## Install (for users, not maintainers)

1. **Dashboard → Plugins → Repositories → Add** then paste:
   `https://raw.githubusercontent.com/TheAceOfficials/BetterPoster-Jellyfin-Minimal/main/manifest.json`
2. **Catalog → Metadata → Better Poster Minimal → Install latest → Restart Jellyfin.**
3. Open a Movie or TV Series → **3-dot menu → Edit Images → Search** to pick a btttr.cc poster.

## License

This plugin code is provided as-is for Jellyfin. All poster overlay artwork is rendered by [btttr.cc](https://btttr.cc) — please support that service.
