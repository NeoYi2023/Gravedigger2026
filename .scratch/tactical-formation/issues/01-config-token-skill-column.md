---
title: TF-01 配置表 + GrantFormationSkill + SkillConfig.FormationId
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.18 魔法书授予
  - SPEC_04 §9.30 TacticalFormationConfig
  - SPEC_04 §9.24 GrantFormationSkill
  - SPEC_04 §9.21 SkillConfig.FormationId
  - SPEC_04 §14.7 Excel 三行表头
approach: A
depends_on:
  - TF-00
---

## 目标

落地 `Combat_TacticalFormationConfig`（Mode1+Mode2 Excel+CSV）；登记 `GrantFormationSkill` handler；`SkillConfig` 增 `FormationId` 列与加载校验。

## 范围

- Excel `战斗_战术阵型配置表_Combat_TacticalFormationConfig.xlsx` + Bake → CSV
- `ConfigCsvRepository` 加载 + FK 校验（`FormationSkillId` ↔ `SkillConfig`）
- `SoldierManufactureMagicBookHook` 实现 `GrantFormationSkill`（Step2 单槽脉冲）
- 样例：1 阵型行 + 1 魔法书行 + `Skill_Form_Wedge` 技能行

## 不做

- 布阵 snap / 战斗移动（TF-03/04）
- Pattern Prefab（TF-02）

## 验收

- [x] Mode1+Mode2 CSV 可加载；缺 FK Warning
- [x] Step2 命中写入 `SoldierSkills`；未命中空 apply
- [x] 只改 Excel 后 Bake；未删三行表头

## 依赖

- TF-00（SPEC 已关）

## 落地摘要

选定方案 A。`TacticalFormationConfigRow` + `ConfigCsvRepository.LoadTacticalFormations`；`SkillConfig.FormationId`；`GrantFormationSkill` 写进既有 Hook。样例 `Form_Wedge_01` / `Skill_Form_Wedge` / `MagicBook_Form_Wedge`。
