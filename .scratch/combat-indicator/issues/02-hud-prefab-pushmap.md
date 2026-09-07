---
title: CI-02 CombatIndicator Prefab + PushMap Combat wire
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.6 UI-033
  - SPEC_03 §3.8 D-089
  - SPEC_03 §3.14
  - SPEC_04 §6
  - SPEC_04 §13
depends_on:
  - 01-config-loader
approach: A
---

# 02 — 共享 HUD Prefab + PushMap 接线

## 目标

实现共享战斗指示器表现层，并仅在 **PushMap Combat** 接线验证。

## 步骤

1. **规则层薄读（PushMapSessionService）：** 只读枚举已登记士兵/怪物（复用 List，禁止每帧 new）：
   - 士兵：`WarriorId, RemainingHp, MaxHp, IsCombatDead, IsPermanentDead, IsRebel`
   - 怪物：`RuntimeId, MonsterId, RemainingHp, MaxHp, IsAlive, IsCombatDead, RevivePhase, RevivesRemaining`
2. **SnapshotBuilder（纯 C#）：**
   - `WarriorPool` → `ClassId`；查 `ClassConfig` / `MonsterConfig`
   - 排序：我方 `ClassLevel`↓ → `TableOrder`↑ → 登记序；敌方 `MonsterType`↓ → `TableOrder`↑ → 登记序
   - 存活计数：忠诚且未永久死亡且 HP>0；可复活怪（`RevivePhase≠None` 或 `RevivesRemaining>0`）算活着
   - 叛变：两侧皆不入
3. **View + Prefab：**
   - `Assets/Prefabs/Combat/CombatIndicatorHud.prefab`（或先 RuntimeFactory）
   - 锚点 `(0.5,1)`，`y=-150`；中央 `HPPK_UI_1` + 左右数字；槽模板 `HPPK_UI_2`/`_3`；死亡叠加 `HPPK_UI_4`
   - 0.2s 抽照；指纹未变只改 tint/数字；进出/死亡到期才 Rebuild
   - 永久死亡：灰 + X，View 记 `expireTime`，0.5s 后移除并重对齐
   - 溢出：半屏宽不够 → 向下换行，行内仍向中心对齐
   - 对象池化格子；`raycastTarget=false`
4. **PushMapStageController：** `Combat` Show / `Prepare`·`Ended` Hide；结算弹窗期间 Hide。

## 验收

- [x] PushMap Combat 顶中出现指示器；Prepare 无（代码接线；Play Mode 负责人勾选）
- [x] 中央数字 = 当前存活数（不含 0.5s 残留格）（逻辑已实现）
- [x] 血色四档正确；死亡格 0.5s 后消失并重对齐（逻辑已实现）
- [x] 排序符合 ClassLevel / MonsterType 规则（Builder）
- [x] 多单位溢出换行（LayoutSide）
- [ ] Profiler：无每帧 List 分配；战斗热路无新增 HP 事件（Play Mode 负责人勾选）

## 交付摘要（2026-09-07）

选定方案：**A**（难度 2）

| 路径 | 说明 |
|------|------|
| `Assets/Scripts/Core/Combat/CombatIndicatorSnapshot.cs` | 薄读 struct + Snapshot |
| `Assets/Scripts/Core/Combat/CombatIndicatorSnapshotBuilder.cs` | 纯 C# 排序 / 存活 / 指纹 |
| `Assets/Scripts/UI/CombatIndicatorHudRuntimeFactory.cs` | Runtime HUD（未固化 Prefab） |
| `Assets/Scripts/UI/CombatIndicatorHudView.cs` | 0.2s 轮询 / 池化 / 死亡 0.5s / 换行 |
| `PushMapSessionService` | `CopyWarrior/MonsterIndicatorReads` + 登记序 |
| `PushMapStageController` | Combat Show；Prepare/Ended/结算 Hide |

SearchExtract 接线见 03。Defend 不做。

选定方案：A（2026-09-07）
