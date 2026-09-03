---
title: LRM-06 运行时读 Prefab 钉点 + 删子关卡 MapPos 列
status: ready for handcheck
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.9
  - SPEC_03 §3.8 D-086
  - SPEC_04 §2
  - SPEC_04 §9.31
  - SPEC_04 §14.7
approach: C
---

## 目标

`LevelRouteSelectView` 按当前 `LevelId` 加载 `LevelRouteMap_{LevelId}`；选项卡钉在对应 `GameplayOptionId` 子节点位置。去掉 Snapshot / Row / Loader 的 `MapPos*`；Excel+CSV 删除 `MapPosX`/`MapPosY` 并 Bake（Mode1+Mode2）。

## 前置

- LRM-05 done（每关地图 Prefab 已有钉点）

## 验收

- [x] 有地图 Prefab：竖滑 + 钉点与 Prefab 一致；可点进玩法（Play Mode 由负责人勾选）
- [x] 缺钉：Warning + 卡片 `(0,0)` 仍可点（代码路径已落地）
- [x] 无地图 Prefab / 无 `RouteMapAssetId`：旧 Stage 行布局
- [x] `SubLevelConfigRow` / Snapshot / `ConfigCsvRepository` 无 MapPos 字段
- [x] Mode1+Mode2 Excel 删列（保留三行表头）→ Bake → CSV 同步

## Unity 手验

1. 若 Resources 副本缺 `.meta`：跑一次 `Gravedigger2026/Level/Ensure LevelRouteMap Prefabs (UI-031)`
2. Mode2 普通进路线：竖滑底图；选项卡中心对齐 Prefab 钉点；可点进玩法
3. 故意删掉某钉 → Warning，卡片在 `(0,0)` 仍可点
4. 无 `RouteMapAssetId` 的关卡仍 Stage 行

## 实现备注

- `LevelRouteSelectView` / `LevelOperationDriver` / `LevelRouteSnapshot`
- 运行时 `Resources.Load("Prefabs/Level/LevelRouteMap_"+LevelId)`；权威源仍 `Assets/Prefabs/Level/`
- Excel：`关卡_子关卡表_Level_SubLevelConfig.xlsx`（根 + Mode2）
- 迁移完成后 CSV 中不再保留 MapPos 列
