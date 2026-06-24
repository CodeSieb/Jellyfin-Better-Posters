#!/usr/bin/env python3
"""Idempotently append a 1.0.4.0 entry to manifest.json with a caller-supplied
UPPERCASE MD5 hex. Re-running is safe — if 1.0.4.0 is already present, the
existing entry is kept (no overwrite).

Usage:
    python update_manifest_1.0.4.0.py <UPPERCASE_MD5_HEX>
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parent
MANIFEST_PATH = PROJECT_ROOT / "manifest.json"
VERSION = "1.0.4.0"

NEW_ENTRY = {
    "version": VERSION,
    "changelog": (
        "Add a URL Patterns Reference panel to the settings page documenting all "
        "seven supported btttr.cc URL variants. Fix GetPosterPath so the path "
        "letter is preserved whenever Genre is on (the previous XOR-style mapping "
        "produced a bare 'poster/' path when both Genre and Rating were on, which "
        "did not match the service's documented 'all combined' pattern "
        "'poster-gqa'). Mirror the same path-letter rule inside the JS preview "
        "builder so the Dashboard URL always matches the URL the plugin actually "
        "composes. No behavior change beyond URL paths now correctly tracking the "
        "service's poster-<X> scheme. Compiled against Jellyfin.Controller / "
        "Jellyfin.Model 10.11.11."
    ),
    "targetAbi": "10.11.11.0",
    "sourceUrl": (
        "https://raw.githubusercontent.com/CodeSieb/Jellyfin-Better-Posters/main/"
        "releases/Jellyfin.Plugin.BetterPosterMinimal-1.0.4.0.zip"
    ),
    "checksum": "REPLACE_AT_RUNTIME",
    "timestamp": "2026-06-24T00:00:00Z",
}


def normalize_md5(raw: str) -> str:
    """Strip whitespace + lowercase → uppercase + reject non-hex."""
    s = "".join(raw.split()).upper()
    if len(s) != 32 or any(c not in "0123456789ABCDEF" for c in s):
        raise ValueError(
            f"Expected a 32-char hex MD5; got: {raw!r} (normalized: {s!r})"
        )
    return s


def main() -> int:
    if len(sys.argv) != 2:
        print(
            "Usage: python update_manifest_1.0.4.0.py <UPPERCASE_MD5_HEX>",
            file=sys.stderr,
        )
        return 2

    md5 = normalize_md5(sys.argv[1])

    if not MANIFEST_PATH.exists():
        print(f"manifest.json not found at {MANIFEST_PATH}", file=sys.stderr)
        return 1

    with MANIFEST_PATH.open("r", encoding="utf-8") as f:
        manifest = json.load(f)

    if not isinstance(manifest, list) or len(manifest) == 0:
        print("manifest.json top-level is not a non-empty array", file=sys.stderr)
        return 1

    versions = manifest[0].setdefault("versions", [])
    existing = [v for v in versions if v.get("version") == VERSION]

    if existing:
        if existing[0].get("checksum", "").upper() == md5:
            print(
                f"manifest.json: versions[{len(versions) - 1}] already "
                f"= {VERSION} with the same MD5 — no change."
            )
            return 0
        # If checksum differs (e.g. a previous failed run), report and exit
        # without overwriting — let the maintainer decide by editing manually.
        print(
            f"manifest.json: versions entry for {VERSION} already exists with "
            f"a DIFFERENT MD5 ({existing[0].get('checksum', '')} vs new {md5}). "
            f"Refusing to overwrite automatically; edit manifest.json by hand "
            f"if you really want to replace it.",
            file=sys.stderr,
        )
        return 3

    entry = dict(NEW_ENTRY)
    entry["checksum"] = md5
    versions.append(entry)
    manifest[0]["versions"] = versions

    with MANIFEST_PATH.open("w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=2, ensure_ascii=False)
        f.write("\n")

    print(
        f"manifest.json: appended versions[{len(versions) - 1}] = {VERSION} "
        f"(checksum={md5})"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
