---
title: Skill_01 格挡（被动受击）
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12 WarriorCombat / SkillCast
  - SPEC_03 §3.8 D-069
  - SPEC_04 §9.21 Skill_01 样例
  - SPEC_04 §9.21b SkillEffect_01_1～_5
selected_approach: B — SoldierSkillCast.TryRollSkill01Block 独立被动钩子；PushMap 受击接线；Defend 本片不接线
---

## 目标

士兵持有 `Skill_01` 时，敌人 **普攻命中 Self** 按等级 **10%～30%** 概率将本次伤害变为 **0**（仍判命中）。

## 范围

- 解析：`CastTarget=Self`；`ExtraActivationCondition=敌人普攻命中Self`
- 接入现有士兵受击 / 怪物 HitConfirm 管线（PushMap，与 SC-01 同战场；Defend 本片不接线）
- 配置驱动；Lv1～5 概率硬映射 `SkillEffect_01_*`（10/15/20/25/30），对齐 `SkillConfig.Description` / Notes

## 不做

- 格挡远程弹道（若非「敌人普攻」语义则 SPEC 另定）
- `Skill_02` 舒适（→ SC-03）

## 验收

- [x] 战士（`Skill_01`）被近战怪普攻时，日志可见概率性 0 伤害且仍算命中
- [x] Lv1 vs Lv5 概率可区分（手验或统计日志）
- [x] D-069 相关项勾选；勾 issue；INDEX SC-02→done

## 依赖

- [SC-01](01-cast-pipeline-skill03.md)（施放/被动基础设施）

## 编码前

- SC-01 done；被动与主动共用事件总线或独立钩子（SC-00 SPEC 为准）→ **选定独立钩子（方案 B）**
