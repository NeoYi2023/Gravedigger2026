#!/usr/bin/env python3
"""Bake MonsterModel_02 art from ZombieMonster2 PNGs + MonsterModel_05 clip template.

When Unity Editor is unavailable, this:
1. Reslices texture .meta (15x8, scale 1.5x from MM05 128px -> 192px cells)
2. Copies Animation Clips + wired controller from MonsterModel_05 with guid remap
3. Removes orphaned .meta for deleted CC-only sheets

Run from repo root: python Tools/bake_monster_model_02_from_zombie.py
"""
from __future__ import annotations

import re
import shutil
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Gravedigger2026"
MM2 = ROOT / "Assets/Art/Characters/Monsters/MonsterModel_02"
MM5 = ROOT / "Assets/Art/Characters/Monsters/MonsterModel_05"
SCALE = 1.5

# MM05 guid -> MM02 guid (Die2/WakeUp created below)
GUID_MAP: dict[str, str] = {
    "fe9a243ff891a994ab609a44a21a1116": "f82f0d672a5eb8b439b8b5bff8b22f5a",  # Attack1
    "0d8351553c984794591330a548d2c9d0": "b03a5b0a32b9bc94dbdd3333b676a698",  # Attack2
    "ce0ac404ed7e4c44186a1e3b23aeba0e": "5fdabf65cfa2bda4d81e59286cd1a016",  # Attack3
    "a5d429e3e17d7b04da1a0663876b1858": "6ee291a981e2b5243859582dce5430c7",  # Attack4
    "941ba68044a1b7d49a5c353aabfcdfdf": "53d4003561fd49a43b0c3cd767a6efa4",  # Attack5
    "7bfb1797abc2c074a8c49c91f92059e0": "01b75d890a5f02140b83dc31fa53385d",  # CrouchRun
    "da448f8e3f35fed43afcb0e57f80612e": "791ce5d185de29242a2114ffd611d2a7",  # Die
    "0207f1961221a384c83f479d066c5970": "220a1f5db0b29a140aa1020d351f7cd5",  # Idle
    "4e9cea42daf21b24e841429b7dfcab3b": "142cb67df8d50b64fb5e3f34a71d7e33",  # Idle2
    "e8ebf7585b08eab4ea3d0975a15fee8a": "3c1ae96ae27e6e949ae56508d7d5f353",  # Run
    "13b7ca80325fd0e4288356dd1f5fb4c6": "031873f77a266814f9431f448acf955e",  # TakeDamage
    "4546c09b6dff6e04aa3a6c73b2d681c3": "f1281a83b2ec8464199e7fcca87bc8f1",  # Taunt
    "add055fcf094bb0479e9d388acac8f59": "3821f33c7503ab547add7604a0ed8cf8",  # Walk
}

PNG_MM5_GUID = {
    "Attack1.png": "fe9a243ff891a994ab609a44a21a1116",
    "Attack2.png": "0d8351553c984794591330a548d2c9d0",
    "Attack3.png": "ce0ac404ed7e4c44186a1e3b23aeba0e",
    "Attack4.png": "a5d429e3e17d7b04da1a0663876b1858",
    "Attack5.png": "941ba68044a1b7d49a5c353aabfcdfdf",
    "CrouchRun.png": "7bfb1797abc2c074a8c49c91f92059e0",
    "Die.png": "da448f8e3f35fed43afcb0e57f80612e",
    "Die2.png": "51c6c7a2005730344a66e4fa4a43e800",
    "Idle.png": "0207f1961221a384c83f479d066c5970",
    "Idle2.png": "4e9cea42daf21b24e841429b7dfcab3b",
    "Run.png": "e8ebf7585b08eab4ea3d0975a15fee8a",
    "TakeDamage.png": "13b7ca80325fd0e4288356dd1f5fb4c6",
    "Taunt.png": "4546c09b6dff6e04aa3a6c73b2d681c3",
    "WakeUp.png": "f15a392e9807eb04c96282b24ac01359",
    "Walk.png": "add055fcf094bb0479e9d388acac8f59",
}

ORPHAN_METAS = [
    "AttackRun.png.meta", "AttackRun2.png.meta", "CrouchIdle.png.meta",
    "Idle3.png.meta", "Idle4.png.meta", "RideIdle.png.meta", "RideRun.png.meta",
    "RunBackwards.png.meta", "Special1.png.meta", "StrafeLeft.png.meta",
    "StrafeRight.png.meta",
]


def unity_guid() -> str:
    return uuid.uuid4().hex


def scale_rect_block(text: str) -> str:
    def repl(m: re.Match[str]) -> str:
        prefix = m.group(1)
        val = float(m.group(2))
        if prefix.strip().endswith("width") or prefix.strip().endswith("height"):
            scaled = val * SCALE
        else:
            scaled = val * SCALE
        if scaled == int(scaled):
            return f"{prefix}{int(scaled)}"
        return f"{prefix}{scaled}"

    # rect: x, y, width, height inside spriteSheet
    pattern = re.compile(r"(^\s+(?:x|y|width|height): )([-+]?\d+(?:\.\d+)?)", re.MULTILINE)
    return pattern.sub(repl, text)


def bake_png_meta(png_name: str, mm2_guid: str) -> None:
    mm5_meta = MM5 / f"{png_name}.meta"
    mm2_meta = MM2 / f"{png_name}.meta"
    if not mm5_meta.is_file():
        raise FileNotFoundError(f"Missing template meta: {mm5_meta}")
    text = mm5_meta.read_text(encoding="utf-8")
    text = re.sub(
        r"^guid: [0-9a-f]+$",
        f"guid: {mm2_guid}",
        text,
        count=1,
        flags=re.MULTILINE,
    )
    text = scale_rect_block(text)
    mm2_meta.write_text(text, encoding="utf-8")
    print(f"  meta: {png_name} -> {mm2_guid}")


def remap_guids(content: str) -> str:
    for old, new in GUID_MAP.items():
        content = content.replace(old, new)
    return content


def copy_anim_tree() -> None:
    src = MM5 / "Animation Clips"
    dst = MM2 / "Animation Clips"
    if dst.exists():
        shutil.rmtree(dst)
    shutil.copytree(src, dst)

    controller_path: Path | None = None
    for path in sorted(dst.rglob("*")):
        if not path.is_file() or path.suffix not in {".anim", ".controller", ".meta"}:
            continue
        text = remap_guids(path.read_text(encoding="utf-8"))
        path.write_text(text, encoding="utf-8")
        if path.suffix == ".controller" and "MonsterModel_05" in path.name:
            controller_path = path

    if controller_path is not None:
        new_path = controller_path.with_name(
            controller_path.name.replace("MonsterModel_05", "MonsterModel_02")
        )
        controller_meta = controller_path.with_suffix(controller_path.suffix + ".meta")
        controller_path.rename(new_path)
        if controller_meta.is_file():
            controller_meta.rename(new_path.with_suffix(new_path.suffix + ".meta"))
        print(f"  controller: {new_path.name}")


def main() -> None:
    if not MM2.is_dir() or not MM5.is_dir():
        raise SystemExit("MonsterModel_02 or MonsterModel_05 folder missing")

    die2_guid = unity_guid()
    wakeup_guid = unity_guid()
    GUID_MAP["51c6c7a2005730344a66e4fa4a43e800"] = die2_guid
    GUID_MAP["f15a392e9807eb04c96282b24ac01359"] = wakeup_guid
    for name in ORPHAN_METAS:
        p = MM2 / name
        if p.is_file():
            p.unlink()
            print(f"  removed {name}")

    print("Baking PNG metas from MonsterModel_05 template (1.5x slice)...")
    for png, mm5_guid in PNG_MM5_GUID.items():
        mm2_guid = GUID_MAP[mm5_guid]
        bake_png_meta(png, mm2_guid)

    print("Copying Animation Clips from MonsterModel_05...")
    copy_anim_tree()

    print("Done. Die2 guid:", die2_guid)
    print("Done. WakeUp guid:", wakeup_guid)


if __name__ == "__main__":
    main()
