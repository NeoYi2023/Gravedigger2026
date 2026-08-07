---
title: MassCombatPathing — Defend 对等接线
status: done
difficulty: 2
demo_scope: authorized
spec_refs:
  - SPEC_03 §3.12 FormationHome / WarriorCombat 寻路
  - SPEC_04 §9.7 GoalKind FormationHome
approach: B
---

## 目标

Defend 士兵/怪物移动与 PushMap 共用同一移动栈语义：追击用 AttackSlot；无候选时 `FormationHome` 直趋或轻量路径；去掉规模方案下的全员重 RVO 依赖。

## 范围

- `WarriorAgentView` / `MonsterAgentView` 改接 Move service（或等价桥）
- `GoalKind=FormationHome` 行为可验
- 与 EngageZone 选敌周期对齐 `TargetRetargetInterval`

## 不做

- PushMap 专属 FlowField 推进（已在 MP-04）
- 200v200 压测场景（→ MP-07）
- 精确 OutsideMap 几何

## 验收

- [x] Defend 开战：士兵追 Engage 内怪走槽位
- [x] 清场后忠诚兵返回 FormationHome
- [x] 返回途中出现新目标会中断返回
- [x] 与 PushMap 无双套互相矛盾的目的地语义

## 依赖

- [05](05-chase-combat-wire.md)

## 实现摘要（MP-06）

- `DefendStageController`：`MassMoveScheduler` + `AttackSlotService`；Combat `TickMassCombatPathing`（槽/steer ≤50 轮转）
- `WarriorAgentView`：Engage 追击→`GoalKind=AttackSlot`；无候选→`FormationHome`；`NavMeshAgent.Move` 跟 steer；无中心 `SetDestination`
- `MonsterAgentView`：追击 `TryClaim` 槽 + scheduler Move；死亡 `Release`/`ReleaseAllForTarget`
- Rebel：追击走 AttackSlot（不回 FormationHome）
- SPEC：v0.73.5

## 手验

1. Defend 开战：忠诚兵追 EngageZone 内怪，落点在 AttackRange 环上（不叠中心）
2. 清光怪后：忠诚兵返回开战 FormationHome 待机
3. 返回途中刷出新怪进 EngageZone：立即中断返回改追槽位
4. 与 PushMap 对照：GoalKind 语义一致（AttackSlot / FormationHome vs Objective）；无全员每帧 CalculatePath
