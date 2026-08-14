# -*- coding: utf-8 -*-
"""Clean Dig grave PNGs for in-game use: strip chroma fringe, orphan black, tight-crop.

Usage (repo root):
  python .scratch/tools/process_grave_sprites.py
  python .scratch/tools/process_grave_sprites.py --only 1-15
  python .scratch/tools/process_grave_sprites.py --dry-run

Backups: .scratch/tools/_grave_q1_q15_backup/Grave_Q{n}.png (first run only).
"""
from __future__ import annotations

import argparse
import shutil
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Gravedigger2026" / "Assets" / "Art" / "Dig" / "Graves"
BACKUP = Path(__file__).resolve().parent / "_grave_q1_q15_backup"
PAD = 2


def has_transparent_neighbor(px, w: int, h: int, x: int, y: int) -> bool:
    for oy in (-1, 0, 1):
        for ox in (-1, 0, 1):
            if ox == 0 and oy == 0:
                continue
            nx, ny = x + ox, y + oy
            if nx < 0 or ny < 0 or nx >= w or ny >= h:
                return True
            if px[nx, ny][3] == 0:
                return True
    return False


def has_art_neighbor(px, w: int, h: int, x: int, y: int) -> bool:
    for oy in (-1, 0, 1):
        for ox in (-1, 0, 1):
            if ox == 0 and oy == 0:
                continue
            nx, ny = x + ox, y + oy
            if nx < 0 or ny < 0 or nx >= w or ny >= h:
                continue
            r, g, b, a = px[nx, ny]
            if a < 128:
                continue
            if max(r, g, b) > 40:
                return True
    return False


def is_greenish(r: int, g: int, b: int) -> bool:
    return g >= 100 and g > r + 30 and g > b + 30


def clean(im: Image.Image) -> tuple[Image.Image, dict[str, int]]:
    im = im.convert("RGBA")
    px = im.load()
    w, h = im.size
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    opx = out.load()
    removed = {"green_fringe": 0, "orphan_black": 0, "low_alpha_noise": 0}
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            if a < 24:
                removed["low_alpha_noise"] += 1
                continue
            edge = has_transparent_neighbor(px, w, h, x, y)
            if is_greenish(r, g, b) and edge and a < 140:
                removed["green_fringe"] += 1
                continue
            if is_greenish(r, g, b) and a < 64:
                removed["green_fringe"] += 1
                continue
            if (
                max(r, g, b) < 22
                and a > 180
                and edge
                and not has_art_neighbor(px, w, h, x, y)
            ):
                removed["orphan_black"] += 1
                continue
            opx[x, y] = (r, g, b, a)
    bbox = out.getbbox()
    if bbox is None:
        return out, removed
    l, t, rgt, btm = bbox
    l = max(0, l - PAD)
    t = max(0, t - PAD)
    rgt = min(w, rgt + PAD)
    btm = min(h, btm + PAD)
    return out.crop((l, t, rgt, btm)), removed


def parse_only(spec: str | None) -> list[int]:
    if not spec:
        return list(range(1, 16))
    out: list[int] = []
    for part in spec.split(","):
        part = part.strip()
        if "-" in part:
            a, b = part.split("-", 1)
            out.extend(range(int(a), int(b) + 1))
        else:
            out.append(int(part))
    return out


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="1-15", help="e.g. 1-15 or 1,3,7")
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()
    ids = parse_only(args.only)
    BACKUP.mkdir(parents=True, exist_ok=True)

    for i in ids:
        src = ART / f"Grave_Q{i}" / f"Grave_Q{i}.png"
        if not src.exists():
            print(f"skip missing {src}")
            continue
        bak = BACKUP / f"Grave_Q{i}.png"
        if not bak.exists() and not args.dry_run:
            shutil.copy2(src, bak)
        im = Image.open(src)
        cleaned, rem = clean(im)
        print(f"Q{i}: {im.size} -> {cleaned.size} rem={rem}")
        if not args.dry_run:
            cleaned.save(src)


if __name__ == "__main__":
    main()
