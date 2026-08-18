---
title: Skill_02 舒适（满血条件增伤）
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12 WarriorCombat / SkillCast
  - SPEC_03 §3.8 D-069
  - SPEC_04 §9.21 Skill_02 样例
  - SPEC_04 §9.21b SkillEffect_02_1～_5
selected_approach: A — SoldierSkillCast.TryGetSkill02OutgoingBonus 结算前倍率钩子；PushMap SettleMonsterDamage；Defend 本片不接线
---

## 目标

士兵持有 `Skill_02` 且 `RemainingHp == MaxHp` 时，Outgoing 伤害 **+5%～+25%**（随等级）。

## 范围

- 解析：`CastTarget=Self`；`ExtraActivationCondition=自身血量=100%`
- 接入士兵普攻 / 技能伤害结算前的倍率通道（与 SC-01 连发叠加规则写清）
- Defend + PushMap 与 SC-01 同范围 → **编码前确认：仅 PushMap**（与 SC-01/SC-02 同范围）

## 不做

- 其它条件增伤技能批量迁移
- Defend 接线

## 验收

- [x] 射手（`Skill_02`）满血时伤害提升可观察；受伤后失效
- [x] Lv1 +5% vs Lv5 +25% 可区分
- [x] D-069 相关项勾选；勾 issue；INDEX SC-03→done

## 依赖

- [SC-01](01-cast-pipeline-skill03.md)

## 编码前

- 难度 **2**；战场 **仅 PushMap**；选定方案 **A**（结算前倍率钩子）
- 叠加：连发 3 击各自走 `SettleMonsterDamage`；每击独立检查 `RemainingHp >= MaxHp`；`本次伤害 = NAP × (1 + bonus)`；不改写存储攻击力；不占普攻/不进 CD/不 roll 失控
