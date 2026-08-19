---
title: Skill_11 灼烧（DoT 叠持续时间）
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12 SkillCast
  - SPEC_03 §3.8 D-073
  - SPEC_04 §9.21 Skill_11 样例
  - SPEC_04 §9.21b OnAaHitApplyBurn
selected_approach: B+ — CombatStatusService.Burn DoT；再施加 RefreshDuration
---

## 目标

`Skill_11` 灼烧 Lv1～5：普攻命中施加灼烧——每 **1s** 造成施术者 **普攻 20%** 伤害，持续 **2～6 秒**；再次施加 **叠加时间**（不叠伤害倍率）。

## 范围

- `CombatStatusService.Burn`：`TickInterval`、`RemainingDuration`、`SourceWarriorId`、`TickDamage = sourceNAP × TickDamageMul`
- Handler @ `OnWarriorAaHitConfirm`；`StackMode=RefreshDuration`（CSV：叠加灼烧的时间）
- Tick 结算走规则层扣怪 HP（可复用 `SettleMonsterDamage` 或独立 DoT 通道；**不**叠 Skill_02 舒适除非 SPEC 写明）
- 无技能 CD（`BaseCooldownSeconds=0`）
- 表：`DurationSeconds=2..6`，`TickDamageMul=0.2`；`EffectImplemented=1`

## 不做

- 灼烧叠层伤害（仅叠时）
- Defend

## 验收

- [x] 火法（`Class_FireMage`）命中后目标持续掉血；再命中延长持续时间
- [x] Lv1 2s vs Lv5 6s
- [x] 勾 issue；INDEX SE-08→done

## 依赖

- [SE-02](02-skill05-tenacity.md) / [SE-03](03-skill06-stun.md)（Status Tick 已通）

## 编码前

- 难度 **2**
