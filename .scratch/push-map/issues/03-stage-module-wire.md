---
title: PushMap — StageModule 与 LevelOperation 接线
status: done
difficulty: 3
demo_scope: deferred
approach: A
spec_refs:
  - SPEC_03 §3.9 GameplayType PushMap
  - SPEC_03 §3.14 PushMapPhase / 复用边界
  - SPEC_03 §3.12 Prepare / Shield / LOC
---

## 目标

`PushMapStageModule`（或等价）识别 `GameplayType=PushMap`；复用 Prepare / FormationEditor / StartBattle / Shield / LossOfControl；按 MapId Instantiate 地图。

## 范围

- LevelOperation → PushMapGameplayConfig
- ModeSelect 模式2 正式路径可后置挂钩（Demo D-044 占位可保留）
- 开战 ≥1；护盾初值；失控 roll

## 不做

- 目标点占领、刷怪、AggroMode、BOSS 通关

## 验收

- [x] 样例关卡阶段可进入 PushMap Prepare→Combat
- [x] Shield / LOC 行为与 Defend 对齐可观察

## 依赖

- [02](02-config-tables.md)

## 编码前

- 难度 3：强制方案比选；本会话仅本片；须 Demo 授权

## 本会话交付（方案 A）

- SPEC：SPEC_04 §6 PushMap Stage 接线（中英）+ SPEC_00 v0.56.0
- Driver：`LevelOperationDriver.TryBuildContext` 支持 `PushMap`（直查 `PushMapGameplayConfig`）；`LevelStageContext.PushMapConfig`；`MapPrefabPaths` 允许 `PushMap_*`
- 规则：`Assets/Scripts/Core/PushMap/PushMapSessionService.cs`（Prepare→Combat；开战≥1；Shield=`ProtagonistMaxHP`；Shield≤0→LevelFailure；锁定 Degree/Tier）+ `PushMapPhase.cs`
- 表现：`Assets/Scripts/Gameplay/PushMap/PushMapStageController.cs`（Instantiate `Maps/{MapId}`，复用共享 FormationEditor；rebel roll 日志可观察）
- 接线：`Assets/Scripts/Core/Level/PushMapStageModule.cs` + `MetaShellController` 注册
- 编辑器：`FormationEditorMode.PushMapPrepare`（开战按钮与 ≥1 校验对齐 Defend）
- 样例：`Level_LevelOperationConfig.csv` 增 `Level_01,4,PushMap,PushMap_01`
- 边界：ModeSelect 模式2 仍占位；无占领/刷怪/AggroMode/BOSS/AirWall NavMesh（PM-04/05/06/08）
- 说明：地图实例经 `DefendPrefabCatalog.TryGetMap(MapId)`，需在 Catalog Maps 绑定 `PushMap_Demo_01`；已提供 Editor 菜单 `Gravedigger2026/PushMap/Ensure Catalog Map Binding`（`PushMapCatalogBinder`，幂等，不改写既有 `Ground_*` 绑定）
