---
title: Skill_08 精英克制（对 Elite 增伤）
status: done
difficulty: 1
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12 SkillCast
  - SPEC_03 §3.8 D-073
  - SPEC_04 §9.21 Skill_08 样例
  - SPEC_04 §9.19 MonsterConfig.MonsterType
  - SPEC_04 §9.21b OutgoingMulVsMonsterType
selected_approach: B+ — OnOutgoingDamageSettle Handler；读 MonsterType.Elite
---

## 目标

`Skill_08` 精英克制 Lv1～5：对 `MonsterType=Elite` 目标 Outgoing × `Mul`（+50%～+90%）。

## 范围

- Handler `OutgoingMulVsMonsterType` @ `OnOutgoingDamageSettle`
- 判定：`ExtraActivationCondition` 对齐 Elite；用 `MonsterConfigRow.MonsterType == Elite`
- Params：`MonsterType=Elite`，`Mul=1.5..1.9`
- 常驻被动；无 CD
- 表 + `EffectImplemented=1`

## 不做

- Boss / Normal 分支（除非 CSV 改）
- Defend

## 验收

- [x] 格斗师（`Class_Brawler`）打 Elite 怪增伤可观察；打 Normal 无加成
- [x] Lv1 +50% vs Lv5 +90%
- [x] 无 Session 硬分支；勾 issue；INDEX SE-05→done

## 依赖

- [SE-01](01-skill04-first-strike.md)（Outgoing Pipeline 已通）

## 编码前

- 难度 **1**
