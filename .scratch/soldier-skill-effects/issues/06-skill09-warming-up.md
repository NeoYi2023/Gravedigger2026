---
title: Skill_09 渐入佳境（叠层攻强）
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12 SkillCast
  - SPEC_03 §3.8 D-073
  - SPEC_04 §9.21 Skill_09 样例
  - SPEC_04 §9.21b StackingOutgoingMulTimed
selected_approach: B+ — 士兵运行时 Stack 状态 + InternalCD 10s 叠层
---

## 目标

`Skill_09` 渐入佳境 Lv1～5：每 **10s** 叠加一层攻击 **+3%/+5%/+7%/+9%/+12%**，**最大总加成 60%**；Outgoing 结算时读取当前总加成。

## 范围

- 士兵状态：`Skill09OutgoingStack` 或通用 `Dictionary<EffectKind, StackState>`（倾向后者便于扩展）
- Handler @ `OnSkillInternalCooldown` 或 Combat Tick：每 10s +1 层，cap `MaxTotalBonus=0.6`
- `@ OnOutgoingDamageSettle` 应用 `1 + currentBonus`；**不改写**存储 `NormalAttackPower`
- Params 按等级：`StackBonus=0.03..0.12`，`MaxTotalBonus=0.6`，`TickSeconds=10`
- CombatSkillIcon：叠层时可 `SkillPersistChanged` 脚标（可选 Demo）
- 表 + `EffectImplemented=1`

## 不做

- 受伤清空层数（CSV 未要求则不做）
- Defend

## 验收

- [x] 狂战士（`Class_Berserker`）战斗越久伤害越高；上限 60%
- [x] Lv1 +3%/层 vs Lv5 +12%/层
- [x] 勾 issue；INDEX SE-06→done

## 依赖

- [SE-01](01-skill04-first-strike.md)

## 编码前

- 难度 **2**
