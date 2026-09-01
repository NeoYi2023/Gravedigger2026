#!/usr/bin/env python3
"""Static verification for MonsterModel_07 ZombieFemale6 bake."""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Gravedigger2026"
MM7 = ROOT / "Assets/Art/Characters/Monsters/MonsterModel_07"
PREFAB = ROOT / "Assets/Prefabs/Defend/Monsters/MonsterModel_07.prefab"
CATALOG = ROOT / "Assets/Settings/Defend/DefendPrefabCatalog.asset"
CSV_PATHS = [
    ROOT / "Assets/ConfigTables/Csv/Defend_MonsterConfig.csv",
    ROOT / "Assets/ConfigTables/Mode2/Csv/Defend_MonsterConfig.csv",
]

PNG_NAMES = [
    "Attack1.png", "Attack2.png", "Attack3.png", "Attack4.png", "Attack5.png",
    "CrouchRun.png", "Die.png", "Die2.png", "Idle.png", "Idle2.png",
    "Run.png", "TakeDamage.png", "Taunt.png", "WakeUp.png", "Walk.png",
]

errors: list[str] = []


def check(name: str, ok: bool, detail: str = "") -> None:
    if ok:
        print(f"  OK  {name}" + (f" — {detail}" if detail else ""))
    else:
        msg = f"FAIL {name}" + (f" — {detail}" if detail else "")
        errors.append(msg)
        print(f"  {msg}")


def main() -> int:
    print("MonsterModel_07 static checks\n")

    for png in PNG_NAMES:
        check(f"PNG {png}", (MM7 / png).is_file())

    controller = list(MM7.glob("Animation Clips/*.controller"))
    check("AnimatorController", len(controller) == 1, controller[0].name if controller else "missing")

    anim_count = len(list((MM7 / "Animation Clips").rglob("*.anim")))
    check("Animation clips", anim_count > 0, f"{anim_count} clips")

    if controller:
        text = controller[0].read_text(encoding="utf-8")
        check("Die2 trigger wired", "m_Name: Die2" in text and "Die2_" in text)

    prefab_text = PREFAB.read_text(encoding="utf-8")
    check("Prefab has Visual child", "m_Name: Visual" in prefab_text)
    check("Prefab no Body cube", "m_Name: Body" not in prefab_text)
    check(
        "Prefab Visual.localScale = 1",
        "m_LocalScale: {x: 1, y: 1, z: 1}" in prefab_text,
    )
    check("Prefab has SpriteRenderer", "SpriteRenderer:" in prefab_text)
    check("Prefab has Animator", "Animator:" in prefab_text)

    catalog_text = CATALOG.read_text(encoding="utf-8")
    check(
        "Catalog binds MonsterModel_07",
        "ModelId: MonsterModel_07" in catalog_text
        and "guid: e79d54b1f1f44eccaa604692e741c74b" in catalog_text,
    )

    for csv_path in CSV_PATHS:
        row = next(
            (line for line in csv_path.read_text(encoding="utf-8").splitlines() if line.startswith("Monster_07,")),
            "",
        )
        check(
            f"CSV {csv_path.parent.name}/{csv_path.name}",
            "Monster_07,MonsterModel_07," in row,
            row[:60] if row else "row missing",
        )

    mm7_files = list(MM7.rglob("*"))
    smallscale_refs = []
    for f in mm7_files:
        if f.is_file() and f.suffix not in {".png", ".meta"}:
            try:
                if "SmallScaleInt" in f.read_text(encoding="utf-8", errors="ignore"):
                    smallscale_refs.append(str(f.relative_to(ROOT)))
            except OSError:
                pass
    check("No SmallScaleInt runtime refs in MM7 art", len(smallscale_refs) == 0, str(smallscale_refs))

    print()
    if errors:
        print(f"{len(errors)} check(s) failed.")
        return 1
    print("All checks passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
