#!/usr/bin/env python3
"""Rebind 8-dir .anim sprites to visual rows for 2D Zombie Pack monster sheets.

Sheets are named {Base}_{row}_{col} with row 0 at the TOP of the PNG:
  0E, 1SE, 2S, 3SW, 4W, 5NW, 6N, 7NE.

AnimatorClipBuilder clip suffixes do not match those rows (e.g. Attack*_SE
binds row 6 = N). Play(\"{base}_SE\") therefore faces north / up-left on screen.

SPEC_04 §15.3 / §15.5 v0.83.97: {Base}_{DIR}.anim must bind the visual row.

Run from repo root:
  python Tools/rebind_zombie_pack_dir_clips.py
"""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Gravedigger2026"
MONSTERS = ROOT / "Assets/Art/Characters/Monsters"

MODELS = (
    "MonsterModel_02",
    "MonsterModel_04",
    "MonsterModel_05",
    "MonsterModel_07",
    "MonsterModel_08",
)

# Clip suffix → PNG row index from the TOP.
VENDOR_ROW = {
    "E": 0,
    "SE": 1,
    "S": 2,
    "SW": 3,
    "W": 4,
    "NW": 5,
    "N": 6,
    "NE": 7,
}

NAME_RE_TMPL = r"^\s+name: {base}_([0-7])_([0-9]+)\s*$"
ID_RE = re.compile(r"^\s+internalID: (-?\d+)\s*$")
FILEID_RE = re.compile(r"\{fileID: (-?\d+),")
SUFFIX_RE = re.compile(r"_(NE|NW|SE|SW|E|W|S|N)$")


def parse_sheet_ids(meta_path: Path, base: str) -> dict[str, tuple[int, int]]:
    name_re = re.compile(NAME_RE_TMPL.format(base=re.escape(base)))
    ids: dict[str, tuple[int, int]] = {}
    pending: tuple[int, int] | None = None
    for line in meta_path.read_text(encoding="utf-8").splitlines():
        m = name_re.match(line)
        if m:
            pending = (int(m.group(1)), int(m.group(2)))
            continue
        if pending is not None:
            im = ID_RE.match(line)
            if im:
                ids[im.group(1)] = pending
                pending = None
    return ids


def invert_ids(ids: dict[str, tuple[int, int]]) -> dict[tuple[int, int], str]:
    return {rc: fid for fid, rc in ids.items()}


def rebind_anim(
    anim_path: Path,
    ids: dict[str, tuple[int, int]],
    by_rc: dict[tuple[int, int], str],
    new_row: int,
) -> int:
    text = anim_path.read_text(encoding="utf-8")
    replacements: list[tuple[str, str]] = []
    seen: set[str] = set()
    for fid in FILEID_RE.findall(text):
        if fid in seen or fid not in ids:
            continue
        seen.add(fid)
        _old_row, col = ids[fid]
        new_id = by_rc.get((new_row, col))
        if not new_id or new_id == fid:
            continue
        replacements.append((fid, new_id))
    if not replacements:
        return 0
    for old_id, new_id in replacements:
        text = text.replace(f"{{fileID: {old_id},", f"{{fileID: {new_id},")
    anim_path.write_text(text, encoding="utf-8")
    return len(replacements)


def main() -> None:
    total_anims = 0
    total_swaps = 0
    already = 0
    missing = []
    for model in MODELS:
        art = MONSTERS / model
        clips_root = art / "Animation Clips"
        if not clips_root.is_dir():
            missing.append(f"{model}: no Animation Clips")
            continue
        for base_dir in sorted(clips_root.iterdir()):
            if not base_dir.is_dir():
                continue
            base = base_dir.name
            meta = art / f"{base}.png.meta"
            if not meta.is_file():
                continue
            ids = parse_sheet_ids(meta, base)
            if not ids:
                missing.append(f"{model}/{base}: no sprite ids")
                continue
            by_rc = invert_ids(ids)
            for anim in sorted(base_dir.glob(f"{base}_*.anim")):
                sm = SUFFIX_RE.search(anim.stem)
                if not sm:
                    continue
                suffix = sm.group(1)
                new_row = VENDOR_ROW[suffix]
                n = rebind_anim(anim, ids, by_rc, new_row)
                if n:
                    total_anims += 1
                    total_swaps += n
                    print(f"{model}/{anim.stem}: -> row {new_row} ({n} sprites)")
                else:
                    already += 1
    print(f"done reboundAnims={total_anims} sprites={total_swaps} alreadyOk={already}")
    if missing:
        print("notes:")
        for s in missing:
            print(" ", s)


if __name__ == "__main__":
    main()
