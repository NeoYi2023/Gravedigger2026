---
title: TFG-01 阵型表增加 FormationLevel 复合主键
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_04 §9.30 TacticalFormationConfig
  - SPEC_04 §9.21 SkillConfig.FormationId
  - SPEC_04 §9.24 GrantFormationSkill
  - SPEC_04 §14.7 Excel 三行表头
  - SPEC_03 §3.8 D-093
approach: A
depends_on:
  - TFG-00
---

## 目标

`Combat_TacticalFormationConfig` 从「一行一个 `FormationId`」改为复合主键 `(FormationId, FormationLevel)`。技能授予仍只认阵型身份。本片不改建组逻辑。

## 需求指令

在 Mode1 与 Mode2 的 `战斗_战术阵型配置表_Combat_TacticalFormationConfig.xlsx` 增加列 `FormationLevel`（整数，≥1）。保留 Excel 第 1～2 行文档头，禁止只改 CSV。Bake 出两边 CSV。

现有 `Form_Wedge_01`、`Form_Wedge_02` 记为 `FormationLevel=1`，Min/Max/Prefab/技能/图标不变。再为楔阵追加一行：`FormationId=Form_Wedge_01`、`FormationLevel=5`、展示与人数与等级 1 相同、`StatModifiers=Stat=Strength|Mul=1.30`。

加载器：

- 重复 `(FormationId, FormationLevel)` 抛错。
- 同 `FormationId` 的展示名、图标、介绍、`FormationSkillId`、Min、Max、`PrefabId` 必须一致；不一致 Warning，几何与展示用最低等级行。
- `TryGetTacticalFormation(formationId)` 改为身份查询（最低等级行），供 `GrantFormationSkill` 取 `FormationSkillId`。
- 新增按等级查询：给定计算等级，返回该 Id 下 ≤ 该值的最大 `FormationLevel` 行；没有则失败。
- 目录用的去重 `FormationId` 列表供 TFG-03 使用。

本片不要改 `EvaluateAndApply` 的自动成组。

## 不做

- 手动建组、存档、卡牌、战斗多中心（TFG-02～05）
- 不改现有 Min/Max 人数去对齐 SPEC 示例与 CSV 的历史差异

## 验收

- [x] Excel 已改且未删前两行表头；Mode1+Mode2 CSV 由 Excel Bake
- [x] 两个相同 `(FormationId, FormationLevel)` 加载失败
- [x] `GrantFormationSkill` 仍用 `FormationId` 命中，不要求调用方传等级
- [x] 计算等级 4 命中楔阵等级 1（1.15）；计算等级 5 或 9 命中等级 5（1.30）；计算等级 0 无行

## 依赖

- TFG-00
