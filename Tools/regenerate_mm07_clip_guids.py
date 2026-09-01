#!/usr/bin/env python3
"""Assign fresh Unity guids to MonsterModel_07 Animation Clips (avoid MM05 duplicates)."""
from __future__ import annotations

import re
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Gravedigger2026"
CLIPS = ROOT / "Assets/Art/Characters/Monsters/MonsterModel_07/Animation Clips"


def new_guid() -> str:
    return uuid.uuid4().hex


def main() -> None:
    if not CLIPS.is_dir():
        raise SystemExit(f"Missing clips folder: {CLIPS}")

    guid_map: dict[str, str] = {}
    meta_files = sorted(CLIPS.rglob("*.meta"))
    for meta in meta_files:
        text = meta.read_text(encoding="utf-8")
        m = re.search(r"^guid: ([0-9a-f]+)$", text, re.MULTILINE)
        if not m:
            continue
        old = m.group(1)
        if old not in guid_map:
            guid_map[old] = new_guid()

    for path in sorted(CLIPS.rglob("*")):
        if not path.is_file() or path.suffix not in {".anim", ".controller", ".meta"}:
            continue
        text = path.read_text(encoding="utf-8")
        for old, new in sorted(guid_map.items(), key=lambda x: -len(x[0])):
            text = text.replace(old, new)
        path.write_text(text, encoding="utf-8")

    print(f"Regenerated {len(guid_map)} guids under {CLIPS}")


if __name__ == "__main__":
    main()
