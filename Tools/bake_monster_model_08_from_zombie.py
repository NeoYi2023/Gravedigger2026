#!/usr/bin/env python3
"""Bake MonsterModel_08 art from ZombieFemale2 PNGs + MonsterModel_05 clip template.

When Unity Editor is unavailable, this:
1. Copies PNGs from SmallScaleInt ZombieFemale2 (1920x1024, 128px/cell)
2. Writes texture .meta from MM05 template (no slice scale — SCALE=1.0)
3. Copies Animation Clips + wired controller from MonsterModel_05 with guid remap

Run from repo root: python Tools/bake_monster_model_08_from_zombie.py
"""
from __future__ import annotations

import re
import shutil
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Gravedigger2026"
MM8 = ROOT / "Assets/Art/Characters/Monsters/MonsterModel_08"
MM5 = ROOT / "Assets/Art/Characters/Monsters/MonsterModel_05"
ZOMBIE_SRC = (
    ROOT / "Assets/SmallScaleInt/2D Zombie Pack 1/Spritesheets/ZombieFemale2"
)

PNG_NAMES = [
    "Attack1.png",
    "Attack2.png",
    "Attack3.png",
    "Attack4.png",
    "Attack5.png",
    "CrouchRun.png",
    "Die.png",
    "Die2.png",
    "Idle.png",
    "Idle2.png",
    "Run.png",
    "TakeDamage.png",
    "Taunt.png",
    "WakeUp.png",
    "Walk.png",
]

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
    "AttackRun.png.meta",
    "AttackRun2.png.meta",
    "CrouchIdle.png.meta",
    "Idle3.png.meta",
    "Idle4.png.meta",
    "RideIdle.png.meta",
    "RideRun.png.meta",
    "RunBackwards.png.meta",
    "Special1.png.meta",
    "StrafeLeft.png.meta",
    "StrafeRight.png.meta",
]

GUID_MAP: dict[str, str] = {}


def unity_guid() -> str:
    return uuid.uuid4().hex


def build_guid_map() -> None:
    GUID_MAP.clear()
    for png_name, mm5_guid in PNG_MM5_GUID.items():
        GUID_MAP[mm5_guid] = unity_guid()


def copy_pngs() -> None:
    if not ZOMBIE_SRC.is_dir():
        raise SystemExit(f"Missing zombie source folder: {ZOMBIE_SRC}")
    MM8.mkdir(parents=True, exist_ok=True)
    for png_name in PNG_NAMES:
        src = ZOMBIE_SRC / png_name
        dst = MM8 / png_name
        if not src.is_file():
            raise FileNotFoundError(f"Missing source PNG: {src}")
        shutil.copy2(src, dst)
        print(f"  png: {png_name}")


def bake_png_meta(png_name: str, mm8_guid: str) -> None:
    mm5_meta = MM5 / f"{png_name}.meta"
    mm8_meta = MM8 / f"{png_name}.meta"
    if not mm5_meta.is_file():
        raise FileNotFoundError(f"Missing template meta: {mm5_meta}")
    text = mm5_meta.read_text(encoding="utf-8")
    text = re.sub(
        r"^guid: [0-9a-f]+$",
        f"guid: {mm8_guid}",
        text,
        count=1,
        flags=re.MULTILINE,
    )
    mm8_meta.write_text(text, encoding="utf-8")
    print(f"  meta: {png_name} -> {mm8_guid}")


def remap_guids(content: str) -> str:
    for old, new in GUID_MAP.items():
        content = content.replace(old, new)
    return content


def copy_anim_tree() -> None:
    src = MM5 / "Animation Clips"
    dst = MM8 / "Animation Clips"
    if dst.exists():
        shutil.rmtree(dst)
    shutil.copytree(src, dst)

    controller_path: Path | None = None
    for path in sorted(dst.rglob("*")):
        if not path.is_file() or path.suffix not in {".anim", ".controller", ".meta"}:
            continue
        text = remap_guids(path.read_text(encoding="utf-8"))
        text = text.replace("MonsterModel_05", "MonsterModel_08")
        path.write_text(text, encoding="utf-8")
        if path.suffix == ".controller" and "MonsterModel_08" in path.name:
            controller_path = path
        elif path.suffix == ".controller" and "MonsterModel_05" in path.name:
            controller_path = path

    if controller_path is not None and "MonsterModel_05" in controller_path.name:
        new_path = controller_path.with_name(
            controller_path.name.replace("MonsterModel_05", "MonsterModel_08")
        )
        controller_meta = controller_path.with_suffix(controller_path.suffix + ".meta")
        controller_path.rename(new_path)
        if controller_meta.is_file():
            controller_meta.rename(new_path.with_suffix(new_path.suffix + ".meta"))
        print(f"  controller: {new_path.name}")
    elif controller_path is not None:
        print(f"  controller: {controller_path.name}")


def main() -> None:
    if not MM5.is_dir():
        raise SystemExit("MonsterModel_05 folder missing")

    build_guid_map()

    for name in ORPHAN_METAS:
        p = MM8 / name
        if p.is_file():
            p.unlink()
            print(f"  removed {name}")

    print("Copying PNGs from ZombieFemale2...")
    copy_pngs()

    print("Baking PNG metas from MonsterModel_05 template (128px, no scale)...")
    for png, mm5_guid in PNG_MM5_GUID.items():
        mm8_guid = GUID_MAP[mm5_guid]
        bake_png_meta(png, mm8_guid)

    print("Copying Animation Clips from MonsterModel_05...")
    copy_anim_tree()

    print("Done. GUID map entries:", len(GUID_MAP))


if __name__ == "__main__":
    main()
