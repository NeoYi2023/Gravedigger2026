# -*- coding: utf-8 -*-
from pathlib import Path

root = Path(r"e:\Work\Cursor\Gravedigger2026\Gravedigger2026")

# SPEC_03 terminology + demo edges if anchors exist
p3 = root / "SPEC_03_GameRules.md"
t3 = p3.read_text(encoding="utf-8")

zh_term_old = (
    "| HitFlash | 受伤闪烁 | PushMap 命中成功后目标模型临时亮色闪烁；怪亮红、兵亮白；共 2 次×0.1s 紧接中间不灭（≈连续亮 0.2s）；过程中再受伤则刷新；见 §3.14。 |\n"
    "| DefendPhase |"
)
zh_term_new = (
    "| HitFlash | 受伤闪烁 | PushMap 命中成功后目标模型临时亮色闪烁；怪亮红、兵亮白；共 2 次×0.1s 紧接中间不灭（≈连续亮 0.2s）；过程中再受伤则刷新；见 §3.14。 |\n"
    "| AllyFootCircle | 友军脚下圈 | Defend / PushMap Combat 中**忠诚存活**士兵脚下绿色描边圆 + 内部黑色半透明（α=**160/255**）；"
    "世界半径=`BodyRadius`；localPos Y=-0.05 Z=-0.2；rotation X=**-30**；Order In Layer=`1`；随士兵移动；叛变/死亡隐藏；"
    "见 §3.12 / §3.14、[SPEC_04 §9.7](SPEC_04_Technical.md)。 |\n"
    "| DefendPhase |"
)

en_term_old = (
    "| HitFlash | 受伤闪烁 | PushMap hit-flash on the target model after a successful hit; monster bright red, soldier bright white; "
    "2×0.1s pulses back-to-back with no off gap (≈0.2s continuous); refresh if hit again mid-flash; §3.14. |\n"
    "| DefendPhase |"
)
en_term_new = (
    "| HitFlash | 受伤闪烁 | PushMap hit-flash on the target model after a successful hit; monster bright red, soldier bright white; "
    "2×0.1s pulses back-to-back with no off gap (≈0.2s continuous); refresh if hit again mid-flash; §3.14. |\n"
    "| AllyFootCircle | 友军脚下圈 | During Defend / PushMap Combat, **loyal living** soldiers show a green-stroke foot circle with "
    "black fill α=**160/255**; world radius=`BodyRadius`; localPos Y=-0.05 Z=-0.2; rotation X=**-30**; Order In Layer=`1`; "
    "follows the soldier; hide on Rebel / CombatDead; §3.12 / §3.14, [SPEC_04 §9.7](SPEC_04_Technical.md). |\n"
    "| DefendPhase |"
)

if "| AllyFootCircle |" not in t3:
    if zh_term_old not in t3:
        print("WARN SPEC_03 zh term anchor missing")
    else:
        t3 = t3.replace(zh_term_old, zh_term_new, 1)
        print("SPEC_03 zh term ok")
    if en_term_old not in t3:
        print("WARN SPEC_03 en term anchor missing")
    else:
        t3 = t3.replace(en_term_old, en_term_new, 1)
        print("SPEC_03 en term ok")

# Insert demo edges after HitFlash demo rows if present
def insert_after(src, needle, line):
    i = src.find(needle)
    if i < 0:
        print("missing", needle[:40])
        return src
    j = src.find("\n", i)
    nxt = src[j + 1 : src.find("\n", j + 1)]
    if "AllyFootCircle" in nxt or "友军脚下圈" in nxt:
        print("skip", needle[:40])
        return src
    return src[: j + 1] + line + src[j + 1 :]

zh_hit = "| Demo 受伤闪烁边界 |"
en_hit = "| Demo HitFlash edge |"
zh_line = (
    "| Demo 友军脚下圈边界 | **AllyFootCircle：** Defend/PushMap Combat **忠诚存活**士兵脚下绿描边 + 内黑 α=**160/255**；"
    "半径=`BodyRadius`；localPos `(0,-0.05,-0.2)`；rotation X=**-30**；Order In Layer=`1`；跟随移动；叛变/死亡隐藏；"
    "见 [SPEC_04 §9.7](SPEC_04_Technical.md) |\n"
)
en_line = (
    "| Demo AllyFootCircle edge | **AllyFootCircle:** Defend/PushMap Combat **loyal living** soldiers: green-stroke foot circle + "
    "black fill α=**160/255**; radius=`BodyRadius`; localPos `(0,-0.05,-0.2)`; rotation X=**-30**; Order In Layer=`1`; "
    "follows movement; hide on Rebel/CombatDead; see [SPEC_04 §9.7](SPEC_04_Technical.md) |\n"
)
t3 = insert_after(t3, zh_hit, zh_line)
t3 = insert_after(t3, en_hit, en_line)
p3.write_text(t3, encoding="utf-8", newline="\n")
print("SPEC_03 done")

# SPEC_04
p4 = root / "SPEC_04_Technical.md"
t4 = p4.read_text(encoding="utf-8")
if "AllyFootCircle" not in t4:
    t4 = t4.replace(
        "仅目标类。禁止运行时引用 `SmallScaleInt/`。",
        "仅目标类。**友军脚下圈 AllyFootCircle：** 忠诚存活士兵脚下绿描边 + 内黑 α160/255（半径=`BodyRadius`，Order In Layer=`1`，"
        "localPos Y=-0.05 Z=-0.2，rotation X=-30）；`WarriorAnimView` 批量改 sortingOrder 时跳过。禁止运行时引用 `SmallScaleInt/`。",
        1,
    )
    t4 = t4.replace(
        "goal-kind only. Do not runtime-reference `SmallScaleInt/`.",
        "goal-kind only. **AllyFootCircle:** loyal living soldiers green-stroke + black fill α160/255 "
        "(radius=`BodyRadius`, Order In Layer=`1`, localPos Y=-0.05 Z=-0.2, rotation X=-30); "
        "`WarriorAnimView` skips when batching sortingOrder. Do not runtime-reference `SmallScaleInt/`.",
        1,
    )
    t4 = t4.replace(
        "- **不做：** 攻击前摇/开火等细态；怪物标签；正式 UI Prefab / 本地化 Key\n- **边界不做（本专题）：**",
        "- **不做：** 攻击前摇/开火等细态；怪物标签；正式 UI Prefab / 本地化 Key\n"
        "- **友军脚下圈 AllyFootCircle（v0.75.33）：**\n"
        "  - 路径：`Assets/Scripts/Gameplay/Combat/AllyFootCircleView.cs`\n"
        "  - 表现：localPos `(0,-0.05,-0.2)`；rotation X=**-30**；绿描边 + 内黑 α=**160/255**；半径=`BodyRadius`；"
        "Order In Layer=`1`；`WarriorAnimView` 批量改 sortingOrder/尸体变暗时跳过\n"
        "  - 接线：`WarriorAgentView` / `PushMapAdvanceView` Bind；Rebel/CombatDead 隐藏\n"
        "- **边界不做（本专题）：**",
        1,
    )
    t4 = t4.replace(
        "- **Out:** attack windup/fire detail; monster labels; formal UI Prefab / i18n keys\n- **Out of scope:** full ORCA;",
        "- **Out:** attack windup/fire detail; monster labels; formal UI Prefab / i18n keys\n"
        "- **AllyFootCircle (v0.75.33):** path `AllyFootCircleView.cs`; localPos `(0,-0.05,-0.2)`; rotation X=**-30**; "
        "fill α=**160/255**; Order In Layer=`1`; `WarriorAnimView` skips batch sortingOrder/corpse darken\n"
        "- **Out of scope:** full ORCA;",
        1,
    )
    p4.write_text(t4, encoding="utf-8", newline="\n")
    print("SPEC_04 done")
else:
    print("SPEC_04 already has AllyFootCircle")

# CONTEXT
pc = root / "CONTEXT.md"
tc = pc.read_text(encoding="utf-8")
if "| AllyFootCircle |" not in tc:
    old = (
        "| HitFlash | 受伤闪烁 | PushMap 命中后模型亮色；怪红/兵白；2×0.1s 紧接不灭；重伤刷新 | "
        "[§3.14](SPEC_03_GameRules.md)、[SPEC_04 §9.22](SPEC_04_Technical.md) |\n"
    )
    # try flexible next line
    i = tc.find("| HitFlash |")
    if i < 0:
        print("CONTEXT HitFlash missing")
    else:
        j = tc.find("\n", i)
        line = (
            "| AllyFootCircle | 友军脚下圈 | Defend/PushMap Combat 忠诚存活士兵脚下绿描边圆 + 内黑 α160/255；"
            "半径=`BodyRadius`；localPos Y=-0.05 Z=-0.2；rotation X=-30；跟随；叛变/死亡隐藏；Order In Layer=`1` | "
            "[§3.12](SPEC_03_GameRules.md)/[§3.14](SPEC_03_GameRules.md)、[SPEC_04 §9.7](SPEC_04_Technical.md) |\n"
        )
        tc = tc[: j + 1] + line + tc[j + 1 :]
        pc.write_text(tc, encoding="utf-8", newline="\n")
        print("CONTEXT done")
else:
    print("CONTEXT already has AllyFootCircle")
