# -*- coding: utf-8 -*-
"""Patch SPEC_03 / SPEC_04 / CONTEXT for OffScreenSpawnHint (UI-034 / D-090)."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def must_replace(text: str, old: str, new: str, label: str) -> str:
    if old not in text:
        raise SystemExit(f"missing: {label}")
    return text.replace(old, new, 1)


def patch_spec_03() -> None:
    p = ROOT / "SPEC_03_GameRules.md"
    t = p.read_text(encoding="utf-8")

    t = must_replace(
        t,
        "| CombatIndicator | 战斗指示器 | PushMap / SearchExtract **Combat** 顶中「血条」HUD（UI-033 / D-089）：中央敌我存活数 + 两侧单位格（职业/怪物简画 + HP% 着色）；低频轮询；死亡格 0.5s 后移除；见 §3.14 / §3.19。 |",
        "| CombatIndicator | 战斗指示器 | PushMap / SearchExtract **Combat** 顶中「血条」HUD（UI-033 / D-089）：中央敌我存活数 + 两侧单位格（职业/怪物简画 + HP% 着色）；低频轮询；死亡格 0.5s 后移除；见 §3.14 / §3.19。 |\n"
        "| OffScreenSpawnHint | 离屏刷怪边缘提示 | PushMap / SearchExtract **Combat** 真实刷怪时：若该组 `SpawnPoint`/`basePos` 在战斗相机视口外，于最靠近方向的屏幕边缘显示 `EnemyAttack_1`，闪烁 `OffScreenSpawnHintDurationSeconds`（默认 1.5s）后消失；Prepare 预览 / 屏内 / 非 Combat 不出；见 §3.14 / §3.19、UI-034 / D-090。 |",
        "zh term",
    )

    t = must_replace(
        t,
        "| CombatIndicator | 战斗指示器 | PushMap / SearchExtract **Combat** top-center unit-status HUD (UI-033 / D-089): center alive counts + per-unit slots (class/monster silhouette + HP% tint); low-frequency poll; dead slot removed after 0.5s; §3.14 / §3.19. |",
        "| CombatIndicator | 战斗指示器 | PushMap / SearchExtract **Combat** top-center unit-status HUD (UI-033 / D-089): center alive counts + per-unit slots (class/monster silhouette + HP% tint); low-frequency poll; dead slot removed after 0.5s; §3.14 / §3.19. |\n"
        "| OffScreenSpawnHint | 离屏刷怪边缘提示 | PushMap / SearchExtract **Combat** real spawns: if the group `SpawnPoint`/`basePos` is outside the combat camera viewport, show `EnemyAttack_1` at the nearest screen edge, blink for `OffScreenSpawnHintDurationSeconds` (default 1.5s) then hide; skip Prepare preview / on-screen / non-Combat; §3.14 / §3.19, UI-034 / D-090. |",
        "en term",
    )

    t = must_replace(
        t,
        "| UI-033 | 战斗指示器 | 已定义（Demo / PushMap + SearchExtract） | Combat 顶中 HUD（锚点 `(0.5,1)`，`anchoredPosition.y=-10`，根 `localScale=0.75`，参考分辨率 1920×1080）：中央 `HPPK_UI_1`（`CenterBg`）**子节点**左右半区=敌我**当前存活数**（字号 BestFit 26～42）；敌方本场尚未出现过 `>0` 且当前为 0 时显示 **`?`**，一旦现身过则后续含清零均显示数字；左侧我方格 `HPPK_UI_2`（右对齐靠中心）、右侧敌方格 `HPPK_UI_3`（左对齐靠中心）；简画图标来自 `ClassConfig` / `MonsterConfig` 的 `SilhouetteIconAssetId`（`Resources/UI/Icons/{Id}`）；HP% 着色 ≥66% 绿 / 33%～66% 橙 / ≤33% 红 / 永久死亡灰+`HPPK_UI_4`（0.5s 后移除并重对齐）；单行超出半屏可用宽则**截断**（不显示第 2～N 行）；低频 0.2s 轮询；Prepare/Ended/结算弹窗期间隐藏；Defend **无**；验收 D-089 |",
        "| UI-033 | 战斗指示器 | 已定义（Demo / PushMap + SearchExtract） | Combat 顶中 HUD（锚点 `(0.5,1)`，`anchoredPosition.y=-10`，根 `localScale=0.75`，参考分辨率 1920×1080）：中央 `HPPK_UI_1`（`CenterBg`）**子节点**左右半区=敌我**当前存活数**（字号 BestFit 26～42）；敌方本场尚未出现过 `>0` 且当前为 0 时显示 **`?`**，一旦现身过则后续含清零均显示数字；左侧我方格 `HPPK_UI_2`（右对齐靠中心）、右侧敌方格 `HPPK_UI_3`（左对齐靠中心）；简画图标来自 `ClassConfig` / `MonsterConfig` 的 `SilhouetteIconAssetId`（`Resources/UI/Icons/{Id}`）；HP% 着色 ≥66% 绿 / 33%～66% 橙 / ≤33% 红 / 永久死亡灰+`HPPK_UI_4`（0.5s 后移除并重对齐）；单行超出半屏可用宽则**截断**（不显示第 2～N 行）；低频 0.2s 轮询；Prepare/Ended/结算弹窗期间隐藏；Defend **无**；验收 D-089 |\n"
        "| UI-034 | 离屏刷怪边缘提示 | 已定义（Demo / PushMap + SearchExtract） | Combat 真实刷怪一组完成后：以该次 `basePos`（SpawnPoint / `ResolveSpawnPosition`）相对战斗相机 `WorldToViewportPoint`；视口外则在最靠近方向的屏幕边缘显示 `EnemyAttack_1`（`Art/UI/Icons/`→`Resources/UI/Icons/EnemyAttack_1`；缺图空框仍闪）；图标不旋转；alpha 周期闪烁，总时长 `OffScreenSpawnHintDurationSeconds`（默认 1.5）；同帧多组可各出一枚；离开 Combat / Ended / UI-017 / UI-032 清掉进行中提示；PushMap **跳过** `PreparePreview`；Defend **无**；验收 D-090 |",
        "zh ui",
    )

    t = must_replace(
        t,
        "| UI-033 | Combat indicator | Defined (Demo / PushMap + SearchExtract) | Combat top-center HUD (anchor `(0.5,1)`, `anchoredPosition.y=-10`, root `localScale=0.75`, ref 1920×1080): center `HPPK_UI_1` (`CenterBg`) **child** left/right-half = **current alive counts** (BestFit font 26–42); enemy shows **`?`** while this battle has never seen `EnemyAliveCount>0` and current is 0, then always numeric (incl. 0); left ally slots `HPPK_UI_2` (right-align toward center), right enemy slots `HPPK_UI_3` (left-align toward center); silhouettes from `ClassConfig` / `MonsterConfig` `SilhouetteIconAssetId` (`Resources/UI/Icons/{Id}`); HP% tint ≥66% green / 33%–66% orange / ≤33% red / permanent-dead gray+`HPPK_UI_4` (remove after 0.5s and re-align); one row exceeding half-screen usable width → **truncate** (do not show rows 2…N); 0.2s poll; hide in Prepare/Ended/settlement; Defend **none**; accept D-089 |",
        "| UI-033 | Combat indicator | Defined (Demo / PushMap + SearchExtract) | Combat top-center HUD (anchor `(0.5,1)`, `anchoredPosition.y=-10`, root `localScale=0.75`, ref 1920×1080): center `HPPK_UI_1` (`CenterBg`) **child** left/right-half = **current alive counts** (BestFit font 26–42); enemy shows **`?`** while this battle has never seen `EnemyAliveCount>0` and current is 0, then always numeric (incl. 0); left ally slots `HPPK_UI_2` (right-align toward center), right enemy slots `HPPK_UI_3` (left-align toward center); silhouettes from `ClassConfig` / `MonsterConfig` `SilhouetteIconAssetId` (`Resources/UI/Icons/{Id}`); HP% tint ≥66% green / 33%–66% orange / ≤33% red / permanent-dead gray+`HPPK_UI_4` (remove after 0.5s and re-align); one row exceeding half-screen usable width → **truncate** (do not show rows 2…N); 0.2s poll; hide in Prepare/Ended/settlement; Defend **none**; accept D-089 |\n"
        "| UI-034 | Off-screen spawn edge hint | Defined (Demo / PushMap + SearchExtract) | After each Combat real spawn group: test `basePos` (SpawnPoint / `ResolveSpawnPosition`) via combat camera `WorldToViewportPoint`; if outside viewport, show `EnemyAttack_1` at nearest screen edge (`Art/UI/Icons/`→`Resources/UI/Icons/EnemyAttack_1`; missing → empty frame still blinks); icon does not rotate; alpha blink for `OffScreenSpawnHintDurationSeconds` (default 1.5); multiple groups may each show one; clear on leave Combat / Ended / UI-017 / UI-032; PushMap **skips** `PreparePreview`; Defend **none**; accept D-090 |",
        "en ui",
    )

    t = must_replace(
        t,
        "| D-089 | 战斗指示器（UI-033 / 方案 A）：PushMap + SearchExtract **仅 Combat** 显示顶中敌我单位格 HUD（`y=-10`，`scale=0.75`）；`CenterBg` 内左右存活数（BestFit 26～42；敌方开场未现身前为 `?`）；简画按职业/怪物表；HP% 四色；永久死亡灰+X 0.5s 后移除重对齐；可复活怪算活着；叛变不入两侧；单行截断（不换行）；0.2s 轮询 Snapshot（不改战斗热路事件）；Defend 不接线 | P1 | TBD（issues `.scratch/combat-indicator/`） |",
        "| D-089 | 战斗指示器（UI-033 / 方案 A）：PushMap + SearchExtract **仅 Combat** 显示顶中敌我单位格 HUD（`y=-10`，`scale=0.75`）；`CenterBg` 内左右存活数（BestFit 26～42；敌方开场未现身前为 `?`）；简画按职业/怪物表；HP% 四色；永久死亡灰+X 0.5s 后移除重对齐；可复活怪算活着；叛变不入两侧；单行截断（不换行）；0.2s 轮询 Snapshot（不改战斗热路事件）；Defend 不接线 | P1 | TBD（issues `.scratch/combat-indicator/`） |\n"
        "| D-090 | 离屏刷怪边缘提示（UI-034 / 方案 A）：PushMap + SearchExtract **仅 Combat 真实刷怪**；判定=该组 `basePos` 视口外；边缘 `EnemyAttack_1` 闪烁 1.5s；Prepare 预览不出；同帧多组独立；离 Combat 清理；Defend 不接线 | P1 | TBD（issues `.scratch/offscreen-spawn-hint/`） |",
        "zh d089",
    )

    t = must_replace(
        t,
        "| D-089 | Combat indicator (UI-033 / Approach A): PushMap + SearchExtract **Combat only** top-center ally/enemy unit-slot HUD (`y=-10`, `scale=0.75`); alive counts inside `CenterBg` left/right halves (BestFit 26–42; enemy `?` until first `>0` this battle); silhouettes from class/monster tables; HP% four tints; permanent-dead gray+X then remove after 0.5s and re-align; revivable monsters count as alive; Rebels excluded from both sides; single-row truncate (no wrap); 0.2s Snapshot poll (no combat hot-path events); Defend unwired | P1 | TBD (issues `.scratch/combat-indicator/`) |",
        "| D-089 | Combat indicator (UI-033 / Approach A): PushMap + SearchExtract **Combat only** top-center ally/enemy unit-slot HUD (`y=-10`, `scale=0.75`); alive counts inside `CenterBg` left/right halves (BestFit 26–42; enemy `?` until first `>0` this battle); silhouettes from class/monster tables; HP% four tints; permanent-dead gray+X then remove after 0.5s and re-align; revivable monsters count as alive; Rebels excluded from both sides; single-row truncate (no wrap); 0.2s Snapshot poll (no combat hot-path events); Defend unwired | P1 | TBD (issues `.scratch/combat-indicator/`) |\n"
        "| D-090 | Off-screen spawn edge hint (UI-034 / Approach A): PushMap + SearchExtract **Combat real spawns only**; gate = group `basePos` outside viewport; edge `EnemyAttack_1` blinks 1.5s; Prepare preview skipped; multi-group independent; clear on leave Combat; Defend unwired | P1 | TBD (issues `.scratch/offscreen-spawn-hint/`) |",
        "en d089",
    )

    insert_zh = (
        "| Demo 离屏刷怪边缘提示边界 | **OffScreenSpawnHint（UI-034 / D-090 / 方案 A）：** PushMap + SearchExtract **仅 `Combat` 真实刷怪**。"
        "一次刷怪请求完成后，用该次 **`basePos`（SpawnPoint / `ResolveSpawnPosition`）** 相对战斗主相机 `WorldToViewportPoint`："
        "视口 `[0,1]²` 内 → 不出；在外 → 钳到最靠近方向的屏幕边缘（边距 `OffScreenSpawnHintEdgeMarginPx`）显示 `EnemyAttack_1`"
        "（`Resources/UI/Icons/EnemyAttack_1`；缺图空框仍闪；图标不旋转）。"
        "Alpha 周期闪烁，总时长 `OffScreenSpawnHintDurationSeconds`（默认 **1.5**；周期 `OffScreenSpawnHintBlinkPeriodSeconds`）。"
        "同帧多组可各出一枚。PushMap **`PreparePreview` 不出**；Prepare / Ended / UI-017 / UI-032 期间清空进行中提示。"
        "Defend **不做**。常量见 [SPEC_04 §9.20b](SPEC_04_Technical.md)。issues `.scratch/offscreen-spawn-hint/` |\n"
    )
    t = must_replace(
        t,
        "| Demo 选敌粘滞边界 | v0.74.10：遇敌选敌带**粘滞迟滞**",
        insert_zh + "| Demo 选敌粘滞边界 | v0.74.10：遇敌选敌带**粘滞迟滞**",
        "zh edge",
    )

    # English §3.14 edge — locate after CombatIndicator English row
    en_ci = "| Demo CombatIndicator edge | **CombatIndicator (UI-033 / D-089 / Approach A):**"
    en_ci_idx = t.find(en_ci)
    if en_ci_idx < 0:
        raise SystemExit("en CI edge missing")
    # Find next table row after this long paragraph
    next_row = t.find("\n| Demo ", en_ci_idx + 10)
    if next_row < 0:
        raise SystemExit("en next row missing")
    insert_en = (
        "\n| Demo OffScreenSpawnHint edge | **OffScreenSpawnHint (UI-034 / D-090 / Approach A):** "
        "PushMap + SearchExtract **Combat real spawns only**. After each spawn request, test **`basePos` "
        "(SpawnPoint / `ResolveSpawnPosition`)** with combat camera `WorldToViewportPoint`: inside `[0,1]²` → no hint; "
        "outside → clamp to nearest screen edge (margin `OffScreenSpawnHintEdgeMarginPx`) and show `EnemyAttack_1` "
        "(`Resources/UI/Icons/EnemyAttack_1`; missing → empty frame still blinks; no rotation). "
        "Alpha blink for `OffScreenSpawnHintDurationSeconds` (default **1.5**; period `OffScreenSpawnHintBlinkPeriodSeconds`). "
        "Multiple groups may each show one. PushMap **skips `PreparePreview`**; clear active hints in Prepare / Ended / UI-017 / UI-032. "
        "Defend **none**. Constants: [SPEC_04 §9.20b](SPEC_04_Technical.md). issues `.scratch/offscreen-spawn-hint/` |"
    )
    t = t[:next_row] + insert_en + t[next_row:]

    t = must_replace(
        t,
        "| 战斗指示器 | 同 §3.14 UI-033 / D-089：`Combat` 显示共享 `CombatIndicatorHud`；Prepare / Ended / UI-017 / UI-032 决策期间隐藏；可复活怪算活着 |",
        "| 战斗指示器 | 同 §3.14 UI-033 / D-089：`Combat` 显示共享 `CombatIndicatorHud`；Prepare / Ended / UI-017 / UI-032 决策期间隐藏；可复活怪算活着 |\n"
        "| 离屏刷怪边缘提示 | 同 §3.14 UI-034 / D-090：Combat 真实刷怪且 `basePos` 视口外 → 边缘 `EnemyAttack_1` 闪 1.5s；Prepare / Ended / UI-017 / UI-032 清空 |",
        "zh se reuse",
    )

    t = must_replace(
        t,
        "| Combat indicator | Same §3.14 UI-033 / D-089: show shared `CombatIndicatorHud` in `Combat`; hide during Prepare / Ended / UI-017 / UI-032 decision; revivable monsters count as alive |",
        "| Combat indicator | Same §3.14 UI-033 / D-089: show shared `CombatIndicatorHud` in `Combat`; hide during Prepare / Ended / UI-017 / UI-032 decision; revivable monsters count as alive |\n"
        "| Off-screen spawn edge hint | Same §3.14 UI-034 / D-090: Combat real spawn with `basePos` off-viewport → edge `EnemyAttack_1` blink 1.5s; clear on Prepare / Ended / UI-017 / UI-032 |",
        "en se reuse",
    )

    # checklist items
    t = must_replace(
        t,
        "- [ ] 战斗指示器 CombatIndicator（UI-033 / D-089）：PushMap + SearchExtract Combat 顶中敌我单位格；issues `.scratch/combat-indicator/`",
        "- [ ] 战斗指示器 CombatIndicator（UI-033 / D-089）：PushMap + SearchExtract Combat 顶中敌我单位格；issues `.scratch/combat-indicator/`\n"
        "- [ ] 离屏刷怪边缘提示 OffScreenSpawnHint（UI-034 / D-090）：屏外 SpawnPoint 边缘 `EnemyAttack_1`；issues `.scratch/offscreen-spawn-hint/`",
        "zh checklist",
    )
    t = must_replace(
        t,
        "- [ ] CombatIndicator (UI-033 / D-089): PushMap + SearchExtract Combat top-center ally/enemy unit slots; issues `.scratch/combat-indicator/`",
        "- [ ] CombatIndicator (UI-033 / D-089): PushMap + SearchExtract Combat top-center ally/enemy unit slots; issues `.scratch/combat-indicator/`\n"
        "- [ ] OffScreenSpawnHint (UI-034 / D-090): off-viewport SpawnPoint edge `EnemyAttack_1`; issues `.scratch/offscreen-spawn-hint/`",
        "en checklist",
    )

    p.write_text(t, encoding="utf-8")
    print("SPEC_03 OK")


def patch_spec_04() -> None:
    p = ROOT / "SPEC_04_Technical.md"
    t = p.read_text(encoding="utf-8")

    zh_block = (
        "**战斗指示器（UI-033 / D-089 / 方案 A）：** 共享 Prefab `Assets/Prefabs/Combat/CombatIndicatorHud.prefab`"
    )
    zh_insert = (
        "**离屏刷怪边缘提示（UI-034 / D-090 / 方案 A）：** 共享 `OffScreenSpawnHintRuntimeFactory` / View（可先 RuntimeFactory）；"
        "ScreenSpaceOverlay 参考 1920×1080；`sortingOrder` 略高于 CombatIndicator（例 70）；`raycastTarget=false`。"
        "判定：刷怪请求的 `basePos` + 战斗相机 `WorldToViewportPoint`；视口外钳到边缘（边距常量）；图标 `Resources/UI/Icons/EnemyAttack_1`；"
        "闪烁时长/周期/边距/尺寸 ← §9.20b `OffScreenSpawnHint*`。"
        "PushMap / SearchExtract StageController 在真实刷怪后 `TryShow`；PreparePreview 跳过；Hide 时 Clear。"
        "Defend **不接线**。issues `.scratch/offscreen-spawn-hint/`。\n\n"
    )
    t = must_replace(t, zh_block, zh_insert + zh_block, "zh §6 CI")

    en_block = (
        "**CombatIndicator (UI-033 / D-089 / Approach A):** shared Prefab `Assets/Prefabs/Combat/CombatIndicatorHud.prefab`"
    )
    en_insert = (
        "**OffScreenSpawnHint (UI-034 / D-090 / Approach A):** shared `OffScreenSpawnHintRuntimeFactory` / View "
        "(RuntimeFactory OK first); ScreenSpaceOverlay ref 1920×1080; `sortingOrder` slightly above CombatIndicator (e.g. 70); "
        "`raycastTarget=false`. Gate: spawn `basePos` + combat camera `WorldToViewportPoint`; clamp to edge when outside; "
        "icon `Resources/UI/Icons/EnemyAttack_1`; duration/period/margin/size ← §9.20b `OffScreenSpawnHint*`. "
        "PushMap / SearchExtract StageControllers `TryShow` after real spawns; skip PreparePreview; Clear on Hide. "
        "Defend **unwired**. issues `.scratch/offscreen-spawn-hint/`.\n\n"
    )
    t = must_replace(t, en_block, en_insert + en_block, "en §6 CI")

    keys_zh = (
        "| `SearchExtractDecisionAutoLeaveSeconds` | 单点自动离开秒 | `3` | **仅** `GatherPointCount=1` 时 LeaveButton 倒计时后自动 Leave |\n"
    )
    keys_zh_new = keys_zh + (
        "| `OffScreenSpawnHintDurationSeconds` | 离屏来袭提示时长 | `1.5` | UI-034：边缘图标闪烁总秒数后消失 |\n"
        "| `OffScreenSpawnHintBlinkPeriodSeconds` | 离屏来袭闪烁周期 | `0.25` | UI-034：alpha 一亮一灭周期秒 |\n"
        "| `OffScreenSpawnHintEdgeMarginPx` | 离屏来袭边距像素 | `48` | UI-034：图标中心距参考分辨率屏幕边的像素边距 |\n"
        "| `OffScreenSpawnHintIconSizePx` | 离屏来袭图标边长 | `72` | UI-034：`EnemyAttack_1` sizeDelta 边长（参考 1920×1080） |\n"
    )
    t = must_replace(t, keys_zh, keys_zh_new, "zh keys")

    keys_en = (
        "| `SearchExtractDecisionAutoLeaveSeconds` | 单点自动离开秒 | `3` | **only** when `GatherPointCount=1`: LeaveButton countdown then auto-Leave |\n"
    )
    # English table may use English Comment - check both
    if keys_en not in t:
        keys_en = (
            "| `SearchExtractDecisionAutoLeaveSeconds` | 单点自动离开秒 | `3` | **Only** `GatherPointCount=1` LeaveButton countdown then auto-Leave |\n"
        )
    # Try find English section key with English comments
    en_anchor = "| `SearchExtractDecisionAutoLeaveSeconds`"
    # Find second occurrence (English §9.20b)
    first = t.find(en_anchor)
    second = t.find(en_anchor, first + 1)
    if second < 0:
        raise SystemExit("en keys second missing")
    line_end = t.find("\n", second)
    line = t[second : line_end + 1]
    keys_en_new = line + (
        "| `OffScreenSpawnHintDurationSeconds` | 离屏来袭提示时长 | `1.5` | UI-034: edge icon blink total seconds then hide |\n"
        "| `OffScreenSpawnHintBlinkPeriodSeconds` | 离屏来袭闪烁周期 | `0.25` | UI-034: alpha on/off period seconds |\n"
        "| `OffScreenSpawnHintEdgeMarginPx` | 离屏来袭边距像素 | `48` | UI-034: icon center margin from screen edge (ref 1920×1080 px) |\n"
        "| `OffScreenSpawnHintIconSizePx` | 离屏来袭图标边长 | `72` | UI-034: `EnemyAttack_1` sizeDelta edge (ref 1920×1080) |\n"
    )
    t = t[:second] + keys_en_new + t[line_end + 1 :]

    # Art exception for EnemyAttack_1 near CombatIndicator / Icons note
    art_needle = "**例外（UI-033 / D-089）：**"
    if art_needle in t:
        t = must_replace(
            t,
            art_needle,
            "**例外（UI-034 / D-090）：** 离屏来袭图标源图 `Assets/Art/UI/Icons/EnemyAttack_1.png`；运行时 `Assets/Resources/UI/Icons/EnemyAttack_1`；`Resources.Load<Sprite>(\"UI/Icons/EnemyAttack_1\")`。\n\n"
            + art_needle,
            "zh art",
        )
    else:
        # fallback near Checkmark / Icons wording
        alt = "运行时图标源 `Art/UI/Icons/` → `Resources/UI/Icons/`（含 Up/Down 与 TipMsg 图标）。"
        if alt in t:
            t = must_replace(
                t,
                alt,
                alt
                + " **UI-034：** `EnemyAttack_1` 同路径（源 `Art/UI/Icons/EnemyAttack_1` → `Resources/UI/Icons/EnemyAttack_1`）。",
                "zh art alt",
            )

    p.write_text(t, encoding="utf-8")
    print("SPEC_04 OK")


def patch_context() -> None:
    p = ROOT / "CONTEXT.md"
    t = p.read_text(encoding="utf-8")
    t = must_replace(
        t,
        "| CombatIndicator | 战斗指示器 | PushMap/SearchExtract Combat 顶中敌我单位格 HUD（`y=-10`/`scale=0.75`）；`CenterBg` 内左右存活数（BestFit 26～42；敌方开场未现身前 `?`）；简画+HP% 四色；死亡 0.5s 后移除；单行截断；0.2s 轮询；UI-033 / D-089 | [§3.14](SPEC_03_GameRules.md)、[§3.19](SPEC_03_GameRules.md)、[SPEC_04 §6](SPEC_04_Technical.md) |",
        "| CombatIndicator | 战斗指示器 | PushMap/SearchExtract Combat 顶中敌我单位格 HUD（`y=-10`/`scale=0.75`）；`CenterBg` 内左右存活数（BestFit 26～42；敌方开场未现身前 `?`）；简画+HP% 四色；死亡 0.5s 后移除；单行截断；0.2s 轮询；UI-033 / D-089 | [§3.14](SPEC_03_GameRules.md)、[§3.19](SPEC_03_GameRules.md)、[SPEC_04 §6](SPEC_04_Technical.md) |\n"
        "| OffScreenSpawnHint | 离屏刷怪边缘提示 | PushMap/SearchExtract Combat 真实刷怪：`basePos` 视口外 → 屏幕边缘 `EnemyAttack_1` 闪 1.5s；Prepare 预览不出；UI-034 / D-090 | [§3.14](SPEC_03_GameRules.md)、[§3.19](SPEC_03_GameRules.md)、[SPEC_04 §6](SPEC_04_Technical.md)/[§9.20b](SPEC_04_Technical.md) |",
        "context",
    )
    p.write_text(t, encoding="utf-8")
    print("CONTEXT OK")


if __name__ == "__main__":
    patch_spec_03()
    patch_spec_04()
    patch_context()
