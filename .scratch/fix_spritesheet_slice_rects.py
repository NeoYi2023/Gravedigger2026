#!/usr/bin/env python3
"""Rewrite Character Creator spritesheet .meta rects to source-size cells (e.g. 128)."""
from __future__ import annotations

import re
import struct
import sys
from pathlib import Path

COLUMNS = 15
ROWS = 8
TOLERANCE = 0.5


def png_size(path: Path) -> tuple[int, int]:
    with path.open("rb") as f:
        sig = f.read(8)
        if sig != b"\x89PNG\r\n\x1a\n":
            raise ValueError(f"not a PNG: {path}")
        while True:
            length_bytes = f.read(4)
            if len(length_bytes) < 4:
                break
            length = struct.unpack(">I", length_bytes)[0]
            chunk_type = f.read(4)
            data = f.read(length)
            f.read(4)  # crc
            if chunk_type == b"IHDR":
                w, h = struct.unpack(">II", data[:8])
                return w, h
    raise ValueError(f"no IHDR: {path}")


def format_num(v: float) -> str:
    if abs(v - round(v)) < 1e-6:
        return str(int(round(v)))
    # Match Unity-ish float style without trailing noise.
    s = f"{v:.5f}".rstrip("0").rstrip(".")
    return s


RECT_BLOCK = re.compile(
    r"(name: (?P<name>\S+)\n"
    r"\s+rect:\n"
    r"\s+serializedVersion: 2\n"
    r"\s+x: (?P<x>[-0-9.]+)\n"
    r"\s+y: (?P<y>[-0-9.]+)\n"
    r"\s+width: (?P<w>[-0-9.]+)\n"
    r"\s+height: (?P<h>[-0-9.]+)\n)",
    re.MULTILINE,
)


def fix_meta(meta_path: Path, columns: int = COLUMNS, rows: int = ROWS) -> bool:
    png_path = meta_path.with_suffix("")
    if not png_path.exists():
        return False

    src_w, src_h = png_size(png_path)
    slice_w = src_w / float(columns)
    slice_h = src_h / float(rows)

    text = meta_path.read_text(encoding="utf-8")
    matches = list(RECT_BLOCK.finditer(text))
    if len(matches) != columns * rows:
        # Not a standard 15x8 sheet — skip.
        return False

    first_w = float(matches[0].group("w"))
    first_h = float(matches[0].group("h"))
    if abs(first_w - slice_w) <= TOLERANCE and abs(first_h - slice_h) <= TOLERANCE:
        return False

    name_re = re.compile(r"^(?P<base>.+)_(?P<row>\d+)_(?P<col>\d+)$")

    def repl(m: re.Match[str]) -> str:
        name = m.group("name")
        nm = name_re.match(name)
        if nm:
            row = int(nm.group("row"))
            col = int(nm.group("col"))
        else:
            # Fall back to encounter order among matches — caller indexes via side channel.
            raise RuntimeError(f"unexpected sprite name: {name} in {meta_path}")

        x = col * slice_w
        y = row * slice_h
        return (
            f"name: {name}\n"
            f"      rect:\n"
            f"        serializedVersion: 2\n"
            f"        x: {format_num(x)}\n"
            f"        y: {format_num(y)}\n"
            f"        width: {format_num(slice_w)}\n"
            f"        height: {format_num(slice_h)}\n"
        )

    new_text, n = RECT_BLOCK.subn(repl, text)
    if n != columns * rows:
        raise RuntimeError(f"replace count {n} != {columns * rows} for {meta_path}")

    meta_path.write_text(new_text, encoding="utf-8")
    return True


def main() -> int:
    root = Path(
        r"e:\Work\Cursor\Gravedigger2026\Gravedigger2026\Gravedigger2026\Assets\Art\Characters"
    )
    fixed = 0
    scanned = 0
    skipped = 0
    errors: list[str] = []

    for meta in sorted(root.rglob("*.png.meta")):
        # Only top-level character sheets (not nested Animation Clips textures).
        if "Animation Clips" in meta.parts:
            continue
        scanned += 1
        try:
            if fix_meta(meta):
                fixed += 1
                print(f"FIXED {meta.relative_to(root)}")
            else:
                skipped += 1
        except Exception as ex:  # noqa: BLE001
            errors.append(f"{meta}: {ex}")

    print(f"scanned={scanned} fixed={fixed} skipped={skipped} errors={len(errors)}")
    for e in errors:
        print("ERROR", e, file=sys.stderr)
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
