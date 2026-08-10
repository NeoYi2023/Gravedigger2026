# -*- coding: utf-8 -*-
from pathlib import Path

# --- SPEC_04 ---
p4 = Path(r"e:\Work\Cursor\Gravedigger2026\Gravedigger2026\SPEC_04_Technical.md")
t4 = p4.read_text(encoding="utf-8")

zh_snip_old = "仅目标类。禁止运行时引用 `SmallScaleInt/`。"
zh_snip_new = (
    "仅目标类。**友军脚下圈 AllyFootCircle：** 忠诚存活士兵脚下绿描边 + 内黑 α160/255"
    "（半径=`BodyRadius`，Order In Layer=`1`，localPos Y=-0.05 Z=-0.2，rotation X=-30）。"
    "禁止运行时引用 `SmallScaleInt/`。"
)
en_snip_old = "goal-kind only. Do not runtime-reference `SmallScaleInt/`."
en_snip_new = (
    "goal-kind only. **AllyFootCircle:** loyal living soldiers get green-stroke foot circle + "
    "black fill α160/255 (radius=`BodyRadius`, Order In Layer=`1`, localPos Y=-0.05 Z=-0.2, "
    "rotation X=-30). Do not runtime-reference `SmallScaleInt/`."
)

if "AllyFootCircle" not in t4.split("仅目标类。")[1][:200]:
    if zh_snip_old not in t4:
        raise SystemExit("zh snip old missing")
    t4 = t4.replace(zh_snip_old, zh_snip_new, 1)
else:
    print("zh summary already has AllyFootCircle?")

# English summary may appear twice? replace carefully once in mass pathing paragraph
if en_snip_old in t4 and "AllyFootCircle:** loyal living" not in t4:
    t4 = t4.replace(en_snip_old, en_snip_new, 1)

zh_block_anchor = "- **不做：** 攻击前摇/开火等细态；怪物标签；正式 UI Prefab / 本地化 Key\n- **边界不做（本专题）：**"
zh_block_insert = (
    "- **不做：** 攻击前摇/开火等细态；怪物标签；正式 UI Prefab / 本地化 Key\n"
    "- **友军脚下圈 AllyFootCircle（v0.75.33）：**\n"
    "  - 路径：`Assets/Scripts/Gameplay/Combat/AllyFootCircleView.cs`\n"
    "  - 表现：士兵根子物体 localPos `(0,-0.05,-0.2)`、rotation X=**-30**；绿色描边环 + 内部黑色半透明"
    "（α=**160/255**）；世界半径 = `BodyRadius`；描边厚度 `max(0.02, BodyRadius×0.12)`；"
    "`SpriteRenderer.sortingOrder`（Order In Layer）=**1**；`WarriorAnimView` 批量改 sortingOrder / "
    "尸体变暗时**跳过**该 Renderer\n"
    "  - 接线：`WarriorAgentView` / `PushMapAdvanceView` 在 `Bind` 时 Ensure + `Bind(bodyRadius)`；"
    "`Rebel`/`SetRebel(true)` / `CombatDead` → `SetVisible(false)`\n"
    "  - **不做：** 怪物/主角/叛变圈；CSV 扩列；Debug 开关\n"
    "- **边界不做（本专题）：**"
)

en_block_anchor = "- **Out:** attack windup/fire detail; monster labels; formal UI Prefab / i18n keys\n- **Out of scope:** full ORCA;"
en_block_insert = (
    "- **Out:** attack windup/fire detail; monster labels; formal UI Prefab / i18n keys\n"
    "- **AllyFootCircle (v0.75.33):**\n"
    "  - Path: `Assets/Scripts/Gameplay/Combat/AllyFootCircleView.cs`\n"
    "  - Presentation: child localPos `(0,-0.05,-0.2)`, rotation X=**-30**; green stroke + black fill "
    "α=**160/255**; world radius = `BodyRadius`; stroke `max(0.02, BodyRadius×0.12)`; "
    "`SpriteRenderer.sortingOrder` (Order In Layer)=**1**; `WarriorAnimView` batch sortingOrder / "
    "corpse darken **skips** this Renderer\n"
    "  - Wire: `WarriorAgentView` / `PushMapAdvanceView` Ensure + `Bind(bodyRadius)` on `Bind`; "
    "`Rebel`/`SetRebel(true)` / `CombatDead` → `SetVisible(false)`\n"
    "  - **Out:** monster/protagonist/rebel circles; CSV columns; Debug toggle\n"
    "- **Out of scope:** full ORCA;"
)

if "AllyFootCircle（v0.75.33）" not in t4 and "AllyFootCircle(v0.75.33)" not in t4:
    if zh_block_anchor not in t4:
        raise SystemExit("zh block anchor missing")
    t4 = t4.replace(zh_block_anchor, zh_block_insert, 1)

if "AllyFootCircle (v0.75.33):" not in t4:
    if en_block_anchor not in t4:
        raise SystemExit("en block anchor missing")
    t4 = t4.replace(en_block_anchor, en_block_insert, 1)

zh_sort_old = "不随 Z 序翻转到角色身后。\n- **尸体叠序（v0.75.14）：**"
zh_sort_new = (
    "不随 Z 序翻转到角色身后。**AllyFootCircle（v0.75.33）** Order In Layer=`1`，"
    "低于角色带与尸体 100；`WarriorAnimView` 不得覆盖。\n- **尸体叠序（v0.75.14）：**"
)
en_sort_old = "never flip behind characters via Z order.\n- **Corpse sorting (v0.75.14):**"
en_sort_new = (
    "never flip behind characters via Z order. **AllyFootCircle (v0.75.33)** Order In Layer=`1`, "
    "below character band and corpse 100; `WarriorAnimView` must not overwrite.\n"
    "- **Corpse sorting (v0.75.14):**"
)

if "AllyFootCircle（v0.75.33）** Order" not in t4:
    if zh_sort_old not in t4:
        raise SystemExit("zh sort missing")
    t4 = t4.replace(zh_sort_old, zh_sort_new, 1)
if "AllyFootCircle (v0.75.33)** Order" not in t4:
    if en_sort_old not in t4:
        raise SystemExit("en sort missing")
    t4 = t4.replace(en_sort_old, en_sort_new, 1)

p4.write_text(t4, encoding="utf-8", newline="\n")
print("SPEC_04 ok")

# --- CONTEXT ---
pc = Path(r"e:\Work\Cursor\Gravedigger2026\Gravedigger2026\CONTEXT.md")
tc = pc.read_text(encoding="utf-8")
ctx_old = (
    "| HitFlash | 受伤闪烁 | PushMap 命中后模型亮色；怪红/兵白；2×0.1s 紧接不灭；重伤刷新 | "
    "[§3.14](SPEC_03_GameRules.md)、[SPEC_04 §9.22](SPEC_04_Technical.md) |\n"
    "| PushMapGameplayConfig |"
)
ctx_new = (
    "| HitFlash | 受伤闪烁 | PushMap 命中后模型亮色；怪红/兵白；2×0.1s 紧接不灭；重伤刷新 | "
    "[§3.14](SPEC_03_GameRules.md)、[SPEC_04 §9.22](SPEC_04_Technical.md) |\n"
    "| AllyFootCircle | 友军脚下圈 | Defend/PushMap Combat 忠诚存活士兵脚下绿描边圆 + 内黑 α160/255；"
    "半径=`BodyRadius`；localPos Y=-0.05 Z=-0.2；rotation X=-30；跟随移动；叛变/死亡隐藏；"
    "Order In Layer=`1` | [§3.12](SPEC_03_GameRules.md)/[§3.14](SPEC_03_GameRules.md)、"
    "[SPEC_04 §9.7](SPEC_04_Technical.md)/[§15.5](SPEC_04_Technical.md) |\n"
    "| PushMapGameplayConfig |"
)
if "| AllyFootCircle |" not in tc:
    if ctx_old not in tc:
        raise SystemExit("ctx anchor missing")
    tc = tc.replace(ctx_old, ctx_new, 1)
    pc.write_text(tc, encoding="utf-8", newline="\n")
    print("CONTEXT ok")
else:
    print("CONTEXT already has AllyFootCircle")
