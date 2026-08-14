---
title: SkillConfig 加载 + ClassConfig.DefaultSkillIds + IconAssetId
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.11 士兵技能授予
  - SPEC_03 §3.8（本片补 D-062）
  - SPEC_04 §9.9b DefaultSkillIds
  - SPEC_04 §9.21
  - SPEC_04 §14
selected_approach: A — 扩现有 CSV；Mode1+Mode2 同步；样例仅战士 DefaultSkillIds=Skill_01
---

## 目标

运行时可查 `SkillConfig(SkillId, SkillLevel)`；`ClassConfigRow` 带解析后的 `DefaultSkillIds`；磁盘列对齐 SPEC_04。

## 范围

- SPEC_03 §3.8 增 **D-062**（P1）：士兵技能垂直 — 表加载 + 池持久化 + Mode1 授予 + Mode2 授予/`SoldierSkillLevelAdd`；状态待实现。SPEC_00 Changelog bump。中英同步。
- `Combat_SkillConfig`（Mode1+Mode2 Excel/CSV）：加列 `IconAssetId`（缺/空=无图）。位置按 SPEC_04 §9.21：`Description` 与 `SkillEffectId` 之间。既有行可空。
- 为手验升等：给 `Skill_01` 补 **Level 2** 行（展示可同 Lv1；`LossOfControlChanceBonus` 可略不同以便日志区分）。其它 Id 不必补多级。
- `Manufacture_ClassConfig`（Mode1+Mode2）：加列 `DefaultSkillIds`。样例仅：
  - Mode1：`Class_Warrior` = `Skill_01`；其余空
  - Mode2：`Class_Warrior` 与 `Class_Warrior_0` = `Skill_01`；其余空
  - **勿**臆造全职业技能表
- 新建 `SkillConfigRow`；`ConfigCsvRepository.LoadSkills`（`TryLoadAll` 在 `LoadClasses` 之后调用；Clear 字典）。复合主键 `(SkillId, SkillLevel)` 重复则抛。
- API：`TryGetSkill(skillId, skillLevel, out row)`；另提供该 `SkillId` 的最小/最大 `SkillLevel` 查询（供 SS-04 钳制）。
- `ClassConfigRow`：解析 `DefaultSkillIds`（空=无；`SkillId` 或 `SkillId|…`；重复保留首次）。加载时未知 SkillId **不抛**（授予时再 Warning）。
- Excel 改完须 Bake 出 CSV（SPEC_04 §14）。

## 不做

- `WarriorInstance` / 持久化（SS-02）
- 制造授予 / 魔法书 Token（SS-03/SS-04）
- 加载 `SkillEffectConfig` 正文、技能施放、Mode1 选技能 UI
- 给所有职业填 DefaultSkillIds

## 验收

- [x] Bake 无报错；运行时 `TryGetSkill("Skill_01", 1)` 与 `("Skill_01", 2)` 成功
- [x] `TryGetClass("Class_Warrior")` 的 DefaultSkillIds 含 `Skill_01`；空列职业长度为 0
- [x] SPEC_03 §3.8 有 D-062（中英）；Changelog 已记
- [x] 勾 issue；INDEX SS-01→done；回复变更文件清单

## 依赖

- SS-00（done）

## 编码前

- 方案 **A** 已锁（INDEX）；可直接编码

## 完成备注

- `IconAssetId` 插在 `Description` 与 `SkillEffectId` 之间；既有行空
- `Skill_01` Lv2 展示同 Lv1；`LossOfControlChanceBonus` 0.03（Lv1=0.02）
- Mode1：`Class_Warrior=Skill_01`；Mode2：`Class_Warrior` 与 `Class_Warrior_0=Skill_01`
- API：`TryGetSkill` + `TryGetSkillLevelRange`（SS-04 钳制）
- SPEC_00 **v0.82.18**；D-062 状态待实现（SS-02～SS-04）
