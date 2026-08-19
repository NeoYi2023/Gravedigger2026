---
title: Skill_07 冰冻（普攻命中 AOE 减速）
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12 SkillCast
  - SPEC_03 §3.8 D-073
  - SPEC_04 §9.21 Skill_07 样例
  - SPEC_04 §9.21b OnAaHitAoeSlow
selected_approach: B+ — CombatStatusService.Slow + 技能 InternalCD 10s
---

## 目标

`Skill_07` 冰冻 Lv1～5：普攻**命中**敌人后，该敌人 **半径 1.5** 内所有敌人 **攻/移速 -50%**，持续 **2～6 秒**；技能 **内部 CD 10s**（`SkillConfig.BaseCooldownSeconds=10`）。

## 范围

- `CombatStatusService.Slow`：分别或合并 `SlowMoveMul`/`SlowAttackMul`（默认均 0.5）
- Handler @ `OnWarriorAaHitConfirm`；触发前检查士兵 `Skill_07` Mode2 CD
- CD 提交：命中后（或 PROC 成功时）进 CD，与 `Skill_03` 独立通道
- 表：`DurationSeconds=2..6`；`EffectImplemented=1`

## 不做

- 冰冻定身（仅减速）
- Defend

## 验收

- [x] 冰法（`Class_IceMage`）命中后 AOE 减速可观察；10s 内不重复 PROC
- [x] 持续 Lv1 2s vs Lv5 6s 可区分
- [x] 勾 issue；INDEX SE-04→done

## 依赖

- [SE-03](03-skill06-stun.md)（AaHit + AOE 模式已验证）

## 编码前

- 难度 **2**
