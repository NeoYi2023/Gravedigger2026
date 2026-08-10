# -*- coding: utf-8 -*-
from pathlib import Path

p = Path(r"e:\Work\Cursor\Gravedigger2026\Gravedigger2026\SPEC_03_GameRules.md")
text = p.read_text(encoding="utf-8")

zh_defend = (
    "| Demo 友军脚下圈边界 | **AllyFootCircle：** Defend Combat 中**忠诚存活**士兵脚下绿描边圆 + 内黑 "
    "α=**160/255**；半径=`BodyAppearanceConfig.BodyRadius`；localPos `(0,-0.05,-0.2)`；"
    "rotation X=**-30**；Order In Layer=`1`；跟随移动；`Rebel` / `CombatDead` 隐藏；怪/主角不显示；"
    "见 [SPEC_04 §9.7](SPEC_04_Technical.md)/[§15.5](SPEC_04_Technical.md) |\n"
)

en_defend = (
    "| Demo AllyFootCircle edge | **AllyFootCircle:** during Defend Combat, **loyal living** soldiers "
    "show green-stroke foot circle + black fill α=**160/255**; radius=`BodyAppearanceConfig.BodyRadius`; "
    "localPos `(0,-0.05,-0.2)`; rotation X=**-30**; Order In Layer=`1`; follows movement; "
    "hide on `Rebel` / `CombatDead`; no circle on monsters/protagonist; see "
    "[SPEC_04 §9.7](SPEC_04_Technical.md)/[§15.5](SPEC_04_Technical.md) |\n"
)

zh_push = (
    "| Demo 友军脚下圈边界 | **AllyFootCircle：** PushMap Combat 中**忠诚存活**士兵脚下绿描边圆 + 内黑 "
    "α=**160/255**（与 §3.12 同契约）；半径=`BodyRadius`；localPos Y=-0.05 Z=-0.2；"
    "rotation X=**-30**；Order In Layer=`1`；跟随移动；`SetRebel` / `CombatDead` 隐藏；怪/主角无圈；"
    "见 [SPEC_04 §9.7](SPEC_04_Technical.md) |\n"
)

en_push = (
    "| Demo AllyFootCircle edge | **AllyFootCircle:** during PushMap Combat, **loyal living** soldiers "
    "show green-stroke foot circle + black fill α=**160/255** (same contract as §3.12); "
    "radius=`BodyRadius`; localPos Y=-0.05 Z=-0.2; rotation X=**-30**; Order In Layer=`1`; "
    "follows movement; hide on `SetRebel` / `CombatDead`; no circle on monsters/protagonist; "
    "see [SPEC_04 §9.7](SPEC_04_Technical.md) |\n"
)

def insert_after_line_containing(src: str, needle: str, new_line: str) -> str:
    if "AllyFootCircle" in src and needle.startswith("| Demo") and "AllyFootCircle" in new_line:
        # avoid duplicate if already present near needle
        pass
    i = src.find(needle)
    if i < 0:
        raise SystemExit(f"needle not found: {needle[:40]}")
    # find end of that table row
    j = src.find("\n", i)
    if j < 0:
        raise SystemExit("newline not found")
    # if next line already has AllyFootCircle for this section, skip
    next_nl = src.find("\n", j + 1)
    next_line = src[j + 1 : next_nl if next_nl > 0 else None]
    if "AllyFootCircle" in next_line or "友军脚下圈" in next_line:
        print(f"skip duplicate after {needle[:30]}")
        return src
    return src[: j + 1] + new_line + src[j + 1 :]

# Find exact row starts
markers = [
    ("| 怪物死亡表现 |", zh_defend),
    ("| Monster death FX |", en_defend),
    ("| Demo 朝向稳定边界 |", zh_push),
    ("| Demo facing stabilization edge |", en_push),
]

for needle, line in markers:
    text = insert_after_line_containing(text, needle, line)
    print("ok", needle[:40])

p.write_text(text, encoding="utf-8", newline="\n")
print("SPEC_03 written")
