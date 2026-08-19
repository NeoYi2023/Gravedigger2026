---
title: Skill_12 瞬移（最远敌 + 背后传送）
status: done
difficulty: 3
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12 SkillCast / 选目标
  - SPEC_03 §3.8 D-073
  - SPEC_04 §9.21 Skill_12 样例
  - SPEC_04 §9.21b RetargetFarthestTeleportBehind
  - SPEC_04 §9.7 AttackSlot / MassMove
selected_approach: B+ — OnWarriorTargetAcquired Handler；规则给目标+落点，View Warp
---

## 目标

`Skill_12` 瞬移 Lv1～5：当士兵**开始寻找新攻击目标**时，改选 **距离自己最远** 的存活敌人，确定后 **瞬移到该敌人背后**；CD **60/50/40/30/20s**。

## 范围

- Handler `RetargetFarthestTeleportBehind` @ `OnWarriorTargetAcquired`（PushMapAdvanceView 重选目标瞬间问规则层）
- 规则：候选 = EngageZone 内存活怪；最远 = XZ 距离最大；背后 = 目标朝向反方向 × `(BodyRadius 和 + ArriveEpsilon)`，再 `NavMesh.SamplePosition`
- 成功：改 `AttackSlot` 目标 + `Warp` 士兵；提交 Mode2 CD
- 失败（无候选 / Sample 失败）：不进 CD，走默认最近目标
- CombatSkillIcon：`SkillIconPopup`
- 表 + `EffectImplemented=1`

## 不做

- 瞬移进墙 / 无视 AirWall（须 Sample 到可走点）
- Defend
- 打断进行中前摇以外的 polish

## 验收

- [x] 影刃（`Class_Shadowblade`）换目标时跳到场上最远怪背后；CD 可观察
- [x] Lv1 60s vs Lv5 20s
- [x] Handler 经 Pipeline；View 不自算「最远」；勾 issue；INDEX SE-09→done

## 依赖

- [SE-01](01-skill04-first-strike.md)

## 编码前

- 难度 **3**（选敌 + Warp + AttackSlot）
