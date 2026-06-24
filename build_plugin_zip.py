"""Build the Jellyfin plugin release zip for the minimal plugin and emit its MD5.

Inputs (existing on disk after `dotnet build -c Release`):
  - bin/Release/net9.0/Jellyfin.Plugin.BetterPosterMinimal.dll

Outputs (written, relative to the repo root):
  - dist/Jellyfin.Plugin.BetterPosterMinimal-1.0.0.0.zip
  - dist/Jellyfin.Plugin.BetterPosterMinimal-1.0.0.0.zip.md5   (32-char hex, lowercase)
"""
from __future__ import annotations

import hashlib
import json
import sys
import zipfile
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parent
DLL_PATH = PROJECT_ROOT / "bin" / "Release" / "net9.0" / "Jellyfin.Plugin.BetterPosterMinimal.dll"
OUT_DIR = PROJECT_ROOT / "releases"
ZIP_NAME = "Jellyfin.Plugin.BetterPosterMinimal-1.0.0.0.zip"

# Inner metadata that Jellyfin reads from inside the plugin zip.
INNER_META = {
    "category": "Metadata",
    "description": (
        "Minimal Jellyfin plugin that surfaces btttr.cc as an Official Remote Image "
        "Provider for Movies and TV Series. IMDb-first with TMDB fallback, optional "
        "scheduled refresh, per-type toggles, settings preview, and reset-to-defaults."
    ),
    "overview": "Just the better posters, from btttr.cc.",
    "owner": "CodeSieb",
    "id": "c2f3aaf3-f591-4a4f-b7e2-a4f1bc9c7d1e",
    "imageUrl": "https://raw.githubusercontent.com/CodeSieb/Jellyfin-Better-Posters/main/Jellyfin-Better-Posters-Image.png",
    "name": "Better Poster Minimal",
    "targetAbi": "10.11.0.0",
    "version": "1.0.0.0",
    "framework": "net9.0",
    "timestamp": "2026-06-24T00:00:00Z",
    "changelog": (
        "Initial Minimal Release. btttr.cc remote image provider for Movies and TV "
        "Series; IMDb-first with TMDB fallback; per-type enable toggles; settings "
        "preview button; reset-to-defaults button; scheduled refresh task."
    ),
}


def main() -> int:
    if not DLL_PATH.exists():
        print(f"DLL not found: {DLL_PATH}", file=sys.stderr)
        print("Run `dotnet build -c Release` first.", file=sys.stderr)
        return 1

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    zip_path = OUT_DIR / ZIP_NAME

    # Fixed ZipInfo timestamps so the zip is byte-identical across runs.
    # Without this, every regeneration produces a different MD5 even though
    # the file contents are unchanged, which makes the manifest checksum
    # field and the actual zip drift apart every time you rebuild.
    FIXED_DATETIME = (1980, 1, 1, 0, 0, 0)  # ZIP epoch

    def info_for(name: str) -> zipfile.ZipInfo:
        info = zipfile.ZipInfo(filename=name, date_time=FIXED_DATETIME)
        info.compress_type = zipfile.ZIP_DEFLATED
        info.external_attr = 0o644 << 16
        return info

    with zipfile.ZipFile(zip_path, "w", compression=zipfile.ZIP_DEFLATED) as zf:
        zf.writestr(
            info_for("meta.json"),
            json.dumps(INNER_META, indent=2, sort_keys=True),
        )
        zf.writestr(
            info_for("Jellyfin.Plugin.BetterPosterMinimal.dll"),
            DLL_PATH.read_bytes(),
        )

    md5_hex = hashlib.md5(zip_path.read_bytes()).hexdigest()

    (OUT_DIR / f"{ZIP_NAME}.md5").write_text(md5_hex, encoding="ascii")

    print(f"Zip written: {zip_path}")
    print(f"Zip size:    {zip_path.stat().st_size} bytes")
    print(f"MD5:         {md5_hex}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
