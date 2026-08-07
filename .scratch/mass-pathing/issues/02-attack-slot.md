---
title: MassCombatPathing — AttackSlot 服务
status: done
difficulty: 3
demo_scope: authorized
spec_refs:
  - SPEC_03 §3.12 AttackSlot
  - SPEC_04 §9.7 AttackSlot 运行时契约
approach: B
---

## 目标

实现 `AttackSlotService`：围绕攻击目标在 `AttackRange` 环上生成/认领可站立槽位，作为追击到达点（非目标中心）。

## 范围

- `Assets/Scripts/Core/Pathing/AttackSlotService.cs`
- `TryClaim(attackerId, targetId, attackRange, targetPos, …) -> worldPos`
- 近战 N=12 / 远程 N=8；`ringRadius = max(0.05, AttackRange − 0.05)`
- 认领表、释放（死亡/换目标）、目标位移 > 0.5 重算
- 可走合法性钩子（接口；SamplePosition 可后接）

## 不做

- 完整命中/前摇结算
- FlowField / LocalDetour
- Stage 接线（→ MP-05）

## 验收

- [x] 多攻击者打同一目标拿到不同槽（在容量内）
- [x] 槽点距目标中心 ≈ ringRadius（误差可接受）
- [x] 释放后槽可被再认领
- [x] 目标大幅移动触发重算

## 依赖

- [00](00-spec-close.md)

## 实现摘要（MP-02）

- `IAttackSlotWalkable` + `StubAttackSlotFullyWalkable`（SamplePosition → 后接）
- `AttackSlotService`：环上 N 槽认领表；≤1 槽/攻击者；`Release` / `ReleaseAllForTarget`；位移 > 0.5 重写环坐标并保留认领索引；来向夹角优先
- `AttackSlotCorrectnessChecks.RunAll()`：无场景自检（异槽 / 环距 / 释放再认领 / 位移重算）
- SPEC_04 §9.7 `TryClaim` 触点补 `targetPos`（v0.73.1）
