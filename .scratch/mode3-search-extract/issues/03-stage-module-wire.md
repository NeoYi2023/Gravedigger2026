---
title: SE-03 GameplayType 接线与 Prepare/开战
status: done
difficulty: 3
demo_scope: out-of-scope
spec_refs:
  - SPEC_04 §6 SearchExtract Stage 接线
  - SPEC_03 §3.19 SearchExtractPhase
  - SPEC_03 §3.9 LevelOperationDriver
approach: A
depends_on:
  - SE-01
---

## 目标

`GameplayType=SearchExtract` → `SearchExtractStageModule` + `IStageModule`；Prepare 布阵 + StartBattle（≥1）；`LevelOperationDriver.TryBuildContext` 解析 §9.32。

## 范围

- `SearchExtractStageModule` / `SearchExtractStageController` 骨架
- `SearchExtractSessionService` 空壳（Phase、CurrentGatherOrder、N）
- 复用 `FormationEditorRoot`（Mode2）；Instantiate 地图；NavMesh+AirWall Bake（对齐 PushMap PM-08）
- `GameplayStateService.SetState(SearchExtract)`；日志可见 LevelId/OptionId

## 不做

- 进圈倒计时 / 刷怪 / UI-032（SE-04+）
- PushMapSessionService 改动

## 验收

- [x] 子关卡选 SearchExtract 可进 Prepare、开战进 Combat（Mode2 `Level_01` 末战分叉 `Opt_SE_Demo_01`）
- [x] 地图与布阵与 PushMap Prepare 同等入口（`FormationEditorMode.SearchExtractPrepare`）
- [x] 无编译错误；最小 handcheck 日志（`[Stage:SearchExtract]` / `[SearchExtractSession]` / `[SearchExtractStage]`）

## 依赖

- SE-01（配置可加载）

## 落地摘要（方案 A 平行克隆）

- 新脚本：`SearchExtractPhase` / `SearchExtractSessionService` / `SearchExtractStageModule` / `SearchExtractStageController`
- Prepare：Instantiate `SearchExtract_Demo_01` + `FormationEditorRoot_Mode2`；开战 ≥1 → Combat 相机 + NavMesh/AirWall Bake + 部署士兵（**不**放 BattleProtagonist、**不**抄 Capture/刷怪）
- `MetaShellController` 注册模块；`TryBuildContext` 写入 `GatherPointCount`
- Mode2 运作表改为 `GameplayOptionId1..5`；`Level_01` Stage5 挂 `Opt_L01_S5_PushMap` + `Opt_SE_Demo_01`；UM `UnlockNext=Opt_L01_S5_PushMap|Opt_SE_Demo_01`
- Changelog SPEC_00 v0.83.78
