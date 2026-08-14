---
title: SPEC 士兵技能规则关闭
status: done
difficulty: 1
demo_scope: out-of-scope
spec_refs:
  - SPEC_03 §3.11
  - SPEC_03 §3.15
  - SPEC_04 §9.9
  - SPEC_04 §9.9b
  - SPEC_04 §9.21
  - SPEC_04 §9.24
  - SPEC_00 v0.82.16
---

## 目标

规则录入完成：`SkillConfig` 为士兵技能权威表；`ClassConfig.DefaultSkillIds`；实例 `SoldierSkills` 制造烘进、`PermanentDeath` 删除；Mode2 `SoldierSkillLevelAdd` 只升已有技能；Mode1 不读魔法书升技能；无经验升级；Demo 仍不施放。

## 状态

**已完成（文档 Agent）**。New Agent **勿再开本片**。

## 后续

编码从 SS-01 起；须负责人授权 Demo 本垂直切片。D-062 由 SS-01 写入 §3.8。
