---
title: Mode1 制造/再造授予 DefaultSkillIds@Lv1
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.11 士兵技能授予
  - SPEC_03 §3.8 D-062
  - SPEC_04 §9.9b
selected_approach: A — ClassId 定稿后授予；抽共享 helper，Mode2 复用
---

## 目标

Mode1 `TryManufacture` / `TryRemanufacture` 在最终 `ClassId` 定稿后写入 `SoldierSkills`（各 DefaultSkillId @ Lv1）。**不读**魔法书、不跑 `SoldierSkillLevelAdd`。

## 范围

- 抽共享授予（建议 `SoldierSkillGrant` 静态类，供 SS-04 复用）：
  - 读 `ClassConfig.DefaultSkillIds`
  - 每 Id：有 `(SkillId, 1)` → 写入 `{ SkillId, SkillLevel=1 }`；无行 → 跳过 + Warning
  - 重复 Id 保留首次
  - 空列 → 空列表
- 调用点：`ManufactureService.BuildWarriorFromAggregate`（制造与再造共用）在 `ClassId` 已 `ResolveInstanceClassId` 之后。
- 制造成功日志附带 `SoldierSkills` 摘要（如 `Skill_01@1`）。
- 可选但推荐：`DefendSessionService` / `PushMapStageController` 的 `skillBonusSum` 改为按实例 `SoldierSkills` 烘进等级查 `SkillConfig.LossOfControlChanceBonus` 之和（SPEC_03 §3.11 `ΣSkillBonus`；灵魂/宝石 `Skills` 并行仍 TBD，本片不加）。无技能则仍为 0。

## 不做

- Mode2 AutoManufacture / 魔法书 Token（SS-04）
- Mode1 选技能 UI、经验升级、技能施放
- 改种族/外观/灵魂定稿顺序

## 验收

- [x] Mode1 用战士灵魂造兵：日志/池内可见 `Skill_01@1`；进档仍在
- [x] 无灵魂 → `Class_Servants` 且 DefaultSkillIds 空 → `SoldierSkills` 空
- [x] 再造走出同一授予路径
- [x] 勾 issue；INDEX SS-03→done；回复变更清单

## 依赖

- SS-02

## 编码前

- 方案 **A** 已锁；可直接编码

## 完成备注

- `SoldierSkillGrant`：`GrantDefaultSkillsAtLevel1`（ClassId 列表 overload 供 SS-04）；无 `(SkillId,1)` → skip + Warning；空列/未知职业 → 空列表
- `ManufactureService.BuildWarriorFromAggregate` 在 `ResolveInstanceClassId` 之后授予；制造/再造日志 `Skills=Skill_01@1`
- Defend/PushMap 开战 roll：`ΣSkillBonus` = `SoldierSkillGrant.SumLossOfControlChanceBonus`（灵魂/宝石 `Skills` 不加）
- SPEC_00 **v0.82.20**；D-062 进行中（SS-01～03 已落地，待 SS-04）
