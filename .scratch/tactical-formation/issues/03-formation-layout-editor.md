---
title: TF-03 布阵 LayoutService + 整阵拖拽
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.18 布阵自动组阵
  - SPEC_04 §6 BattleFormationService / FormationEditorController
  - SPEC_04 §9.30 TacticalFormationLayoutService
approach: A
depends_on:
  - TF-01
  - TF-02
---

## 目标

`TacticalFormationLayoutService`：布阵变更后评估 ≥Min snap / <Min revert；组阵成员整阵拖拽；覆盖职业区坐标。

## 范围

- 纯 C# `TacticalFormationLayoutService.EvaluateAndApply`
- 接线点：`FormationEditorController`（手动上/改/下阵）、`AutoFormationDeployService.DeployBatch` 后、一键上阵后
- 质心 + 地图默认朝向放置；稳定排序截断 `MaxMemberCount`
- 拖拽：命中组阵成员 → 移动整阵中心，更新全部 `TrySetPosition`
- 不足 Min：退回 `FormationClassZone` 螺旋（复用 D-052 算法）

## 不做

- Combat 虚拟中心（TF-04）
- 属性 overlay（TF-05）

## 验收

- [x] UM/Defend/PushMap Prepare：3+ 同阵型技能兵自动 wedge snap
- [x] 下阵至 <Min 自动 revert 职业区
- [x] 组阵成员不可单独拖散

## 依赖

- TF-01、TF-02

## 落地摘要

选定方案 A；难度 2。`TacticalFormationLayoutService` 会话成员集；`EvaluateAndApply` 按 `SkillConfig.FormationId` 分组，≥Min 质心+朝向 snap Pattern 槽位（PushMap 首 Objective / Defend EngageZone / 其余 +Z）；<Min 复用 `FormationZoneSpiralSearch` 回职业区。`FormationEditorController` 命中成员整阵平移中心（不重算朝向）；`AutoFormationDeployService.DeployBatch` 与一键上阵后评估。`BattleFormationService.ApplyPositionBatch` 一次 persist/notify。
