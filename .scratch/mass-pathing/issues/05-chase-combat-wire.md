---
title: MassCombatPathing — 追击/交战接线（士兵+怪物）
status: done
difficulty: 3
demo_scope: authorized
spec_refs:
  - SPEC_03 §3.12 AttackSlot / GoalKind / TargetRetargetInterval
  - SPEC_03 §3.14 遇敌后改 AttackSlot
  - SPEC_04 §9.7 AttackSlot / Move service
approach: B
---

## 目标

士兵与怪物在确定攻击目标后，以 **AttackSlot** 为 `DesiredDestination`，直线趋近 + LocalDetour；按 `TargetRetargetInterval` 重算槽；规则层只出目标 ID + GoalKind。

## 范围

- PushMap：忠诚兵从推进切到交战时 `GoalKind=AttackSlot`
- `PushMapMonsterAgentView` /（必要时）Defend 怪：追击目的地改为槽位
- `AttackSlotService` 认领/释放接到单位生死与换目标
- 分帧：每帧槽位重算 ≤ 50（轮转）
- 保留既有 AggroMode / 命中方案边界（本片不扩技能）

## 不做

- 完整 WarriorCombat 未完成的命中 polish
- Defend FormationHome 完整迁移（→ MP-06 可顺带轻量）
- 压测（→ MP-07）

## 验收

- [x] 多兵打同一怪：落点分散在环上，不叠死中心
- [x] 目标移动超阈后槽更新
- [x] 怪追士兵同样走槽位（或 ChaseAnchor→Slot 管线一致）
- [x] 无全员每帧 CalculatePath

## 依赖

- [02](02-attack-slot.md)
- [03](03-local-detour-spatial-hash.md)
- [04](04-pushmap-advance-wire.md)

## 实现摘要（MP-05）

- Core：`GoalKind`；`MassMoveScheduler.SetGoal(Objective|AttackSlot)` + DetourGroup 友军过滤；槽/steer 分帧 ≤50
- `PushMapStageController`：`AttackSlotService`；`TickAttackSlotGoals` 轮转认领/释放；士兵+怪进 Scheduler
- `PushMapAdvanceView`：遇敌→AttackSlot Move（停跟场）；离开恢复 Objective；死亡/`OnDisable` Release
- `PushMapMonsterAgentView`：追击 `TryClaim` 槽 + scheduler Move；AggroMode 保留；无 `SetDestination(中心)`
- Defend 怪 / FormationHome → MP-06
- SPEC：v0.73.4

## 手验

1. PushMap 开战多忠诚兵贴近同一怪：落点分散在 AttackRange 环上，不叠目标中心
2. 拖动/移动目标怪：槽随位移 >0.5 重算（AttackSlotService）
3. ActiveChase 怪追士兵：走向士兵环上槽位，非中心叠压
4. Profiler/代码：无全员每帧 `CalculatePath` / `SetDestination(Objective|中心)`
