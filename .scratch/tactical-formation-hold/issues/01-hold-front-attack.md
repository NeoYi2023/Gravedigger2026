---
title: TFH-01 守槽并只攻击眼前敌人
status: done
difficulty: 3
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.18 TacticalFormation
  - SPEC_03 §3.8 D-094
  - SPEC_04 §9.7 战术阵型运行时契约
  - SPEC_03 §3.12 FormationSlot
  - SPEC_03 §3.14 士兵推进
approach: A
depends_on:
  - TFH-00
---

## 目标

推图、防守、搜打撤里，已入组士兵不再离开槽位去围敌人。走到槽上之后，只攻击阵型朝向前方 90° 且进入自己攻击距离的敌人。

## 需求指令

先读 TFH-00 写完的 SPEC，再改代码。规则层不要直接改 `Transform`。

`TacticalFormationCombatGoalPolicy`（或同级纯 C#）增加前方扇形判定：轴 = 该组 `FacingYawDegrees`，全角 90°。半角不要写死在三个 Stage 里；放进 `TacticalFormationMoveParams`，缺省 90，Pattern 上可序列化，以便以后改角度时不动 Stage。

PushMap `RefreshSoldierSlotGoal`、Defend 与 SearchExtract 的对应接敌函数：成员不再 `AttackSlotService.TryClaim`，不再 `ClampAttackSlot`。

- 还没进入 `SlotArriveEpsilon`：`GoalKind=FormationSlot`，不暂停。
- 已在槽上，扇形内且 `CombatReach` 判定进入 `AttackRange`：保持 `FormationSlot`（目的地仍是槽位世界点），暂停位移，沿用现在的停步挥刀命中。多个敌人取扇形内最近。
- 否则：保持 `FormationSlot` 并不暂停。释放该成员已认领的 AttackSlot，避免占着别人的环位。

未入组士兵的 AttackSlot / Objective / Home 原样保留。Rebel 已离组，不走本分流。

`LeashRadius` 本片不删除、不改预制体数值，只是成员选敌不再读它。

Correctness 补一条：成员在槽上，敌人在正前方射程内 → 暂停且 Goal 仍是 FormationSlot；敌人在正侧方（超过 45°）即使贴脸 → 不暂停、Goal 仍是 FormationSlot。

## 不做

- 软碰撞锚点（TFH-02）
- 死亡换槽（TFH-03）
- 改 `StatModifiers` 数值（TFH-04）
- 未入组士兵改为守桩

## 验收

- [ ] 推图接敌时组成员脚下任务标签保持「阵型」，不会变成「追击」后拉开楔形
- [ ] 敌人从正前方进入攻击距离：人停在槽上出刀；敌人从侧后方贴过来：人留在槽上不出击
- [ ] 组还在走向槽位时，先到槽再打，不在半路停住
- [ ] 防守与搜打撤同样守槽（不能只改推图）
- [ ] 没进组的士兵仍然会追击

## 依赖

- TFH-00（SPEC 已改成守槽口径）
