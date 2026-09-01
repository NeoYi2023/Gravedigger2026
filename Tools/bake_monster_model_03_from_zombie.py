#!/usr/bin/env python3
"""Bake MonsterModel_03 art from ZombieMonster3 PNGs + MonsterModel_02 clip template.

When Unity Editor is unavailable, this:
1. Copies vendor PNGs from SmallScaleInt/ZombieMonster3
2. Copies texture .meta from MonsterModel_02 with guid remap to MonsterModel_03
3. Copies Animation Clips + wired controller from MonsterModel_02 with guid remap
4. Removes orphaned CC-only sheets and clip folders

Run from repo root: python Tools/bake_monster_model_03_from_zombie.py
"""
from __future__ import annotations

import re
import shutil
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Gravedigger2026"
VENDOR = ROOT / "Assets/SmallScaleInt/2D Zombie Pack 1/Spritesheets/ZombieMonster3"
MM2 = ROOT / "Assets/Art/Characters/Monsters/MonsterModel_02"
MM3 = ROOT / "Assets/Art/Characters/Monsters/MonsterModel_03"

ZOMBIE_PNGS = (
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
)

CC_ONLY_PNGS = (
    "AttackRun.png",
    "AttackRun2.png",
    "CrouchIdle.png",
    "Idle3.png",
    "Idle4.png",
    "RideIdle.png",
    "RideRun.png",
    "RunBackwards.png",
    "Special1.png",
    "StrafeLeft.png",
    "StrafeRight.png",
)

CC_ONLY_CLIP_FOLDERS = (
    "AttackRun",
    "AttackRun2",
    "CrouchIdle",
    "Idle3",
    "Idle4",
    "RideIdle",
    "RideRun",
    "RunBackwards",
    "Special1",
    "StrafeLeft",
    "StrafeRight",
)


def unity_guid() -> str:
    return uuid.uuid4().hex


def read_guid(meta_path: Path) -> str:
    text = meta_path.read_text(encoding="utf-8")
    match = re.search(r"^guid: ([0-9a-f]+)$", text, re.MULTILINE)
    if not match:
        raise ValueError(f"No guid in {meta_path}")
    return match.group(1)


def write_guid(meta_path: Path, guid: str) -> None:
    text = meta_path.read_text(encoding="utf-8")
    text = re.sub(
        r"^guid: [0-9a-f]+$",
        f"guid: {guid}",
        text,
        count=1,
        flags=re.MULTILINE,
    )
    meta_path.write_text(text, encoding="utf-8")


def build_guid_map() -> dict[str, str]:
    guid_map: dict[str, str] = {}

    for png in ZOMBIE_PNGS:
        mm2_meta = MM2 / f"{png}.meta"
        if not mm2_meta.is_file():
            raise FileNotFoundError(f"Missing MM02 template meta: {mm2_meta}")
        mm2_guid = read_guid(mm2_meta)
        mm3_meta = MM3 / f"{png}.meta"
        if mm3_meta.is_file():
            mm3_guid = read_guid(mm3_meta)
        else:
            mm3_guid = unity_guid()
        guid_map[mm2_guid] = mm3_guid

    for meta_path in sorted(MM2.rglob("*.meta")):
        if meta_path.parent == MM2 and meta_path.name in {f"{p}.meta" for p in ZOMBIE_PNGS}:
            continue
        mm2_guid = read_guid(meta_path)
        if mm2_guid not in guid_map:
            guid_map[mm2_guid] = unity_guid()

    return guid_map


def remap_guids(content: str, guid_map: dict[str, str]) -> str:
    for old, new in guid_map.items():
        content = content.replace(old, new)
    return content


def copy_vendor_pngs() -> None:
    if not VENDOR.is_dir():
        raise FileNotFoundError(f"Missing vendor folder: {VENDOR}")
    for png in ZOMBIE_PNGS:
        src = VENDOR / png
        dst = MM3 / png
        if not src.is_file():
            raise FileNotFoundError(f"Missing vendor PNG: {src}")
        shutil.copy2(src, dst)
        print(f"  png: {png}")


def bake_png_metas(guid_map: dict[str, str]) -> None:
    for png in ZOMBIE_PNGS:
        mm2_meta = MM2 / f"{png}.meta"
        mm3_meta = MM3 / f"{png}.meta"
        text = mm2_meta.read_text(encoding="utf-8")
        text = remap_guids(text, guid_map)
        mm3_meta.write_text(text, encoding="utf-8")
        print(f"  meta: {png} -> {read_guid(mm3_meta)}")


def remove_cc_orphans() -> None:
    for png in CC_ONLY_PNGS:
        for path in (MM3 / png, MM3 / f"{png}.meta"):
            if path.is_file():
                path.unlink()
                print(f"  removed {path.name}")

    clips_root = MM3 / "Animation Clips"
    if clips_root.is_dir():
        for folder in CC_ONLY_CLIP_FOLDERS:
            folder_path = clips_root / folder
            if folder_path.is_dir():
                shutil.rmtree(folder_path)
                meta_path = clips_root / f"{folder}.meta"
                if meta_path.is_file():
                    meta_path.unlink()
                print(f"  removed clip folder {folder}")

        for legacy in clips_root.glob("App_10_*"):
            if legacy.is_file():
                legacy_meta = legacy.with_suffix(legacy.suffix + ".meta")
                legacy.unlink()
                if legacy_meta.is_file():
                    legacy_meta.unlink()
                print(f"  removed legacy {legacy.name}")


def copy_anim_tree(guid_map: dict[str, str]) -> None:
    src = MM2 / "Animation Clips"
    dst = MM3 / "Animation Clips"
    if dst.exists():
        shutil.rmtree(dst)
    shutil.copytree(src, dst)

    controller_path: Path | None = None
    for path in sorted(dst.rglob("*")):
        if not path.is_file() or path.suffix not in {".anim", ".controller", ".meta"}:
            continue
        text = remap_guids(path.read_text(encoding="utf-8"), guid_map)
        path.write_text(text, encoding="utf-8")
        if path.suffix == ".controller" and "MonsterModel_02" in path.name:
            controller_path = path

    if controller_path is not None:
        new_path = controller_path.with_name(
            controller_path.name.replace("MonsterModel_02", "MonsterModel_03")
        )
        controller_meta = controller_path.with_suffix(controller_path.suffix + ".meta")
        controller_path.rename(new_path)
        if controller_meta.is_file():
            controller_meta.rename(new_path.with_suffix(new_path.suffix + ".meta"))
        print(f"  controller: {new_path.name}")

    for legacy in dst.glob("App_10_*"):
        if legacy.is_file():
            legacy_meta = legacy.with_suffix(legacy.suffix + ".meta")
            legacy.unlink()
            if legacy_meta.is_file():
                legacy_meta.unlink()
            print(f"  removed legacy {legacy.name}")


def main() -> None:
    if not MM2.is_dir() or not MM3.is_dir():
        raise SystemExit("MonsterModel_02 or MonsterModel_03 folder missing")

    print("Building guid map (MM02 -> MM03)...")
    guid_map = build_guid_map()

    print("Copying vendor PNGs...")
    copy_vendor_pngs()

    print("Removing CC-only orphans...")
    remove_cc_orphans()

    print("Baking PNG metas from MonsterModel_02 template...")
    bake_png_metas(guid_map)

    print("Copying Animation Clips from MonsterModel_02...")
    copy_anim_tree(guid_map)

    print("Done.")


if __name__ == "__main__":
    main()
