---
title: SE-02 样例地图搜集点标记
status: done
difficulty: 2
demo_scope: out-of-scope
spec_refs:
  - SPEC_03 §3.19 地图标记
  - SPEC_04 §13 ObjectivePoint CaptureZone SpawnPoint AirWall
  - SPEC_03 §3.14 作者硬约束
  - SPEC_04 §9.32 MapId SearchExtract_*
approach: B
depends_on:
  - SE-00
---

## 目标

为手验准备地图 Prefab：≥2 个 `ObjectivePoint`+`CaptureZone`、关联 `SpawnPoint`、可走 `AirWall` 边界；Objective 不在墙内。

## 范围

- 复用或复制 `PushMap_Demo_*` / `Ground_*` 为 `SearchExtract` 样例地图（或工作坊指定 MapId）
- `ObjectiveOrder` 1..N；`CaptureZone.Radius` 默认 2
- `SpawnPointId` 与 §9.33 样例行对齐
- Editor 菜单或 AssetBuilder（可选）校验 Objective 不在 AirWall OBB 内

## 不做

- Session / 刷怪逻辑
- 新玩法专属网格（仍 Isometric Tilemap）

## 验收

- [x] Prefab 路径 `Assets/Prefabs/Maps/{MapId}.prefab`
- [x] Gizmo/手验：至少 2 搜集点 + 每点至少 1 SpawnPoint
- [x] 与 PushMap 标记组件复用，无 duplicate 语义组件

## 依赖

- SE-00

## 落地摘要（方案 B）

- 独立图 `Assets/Prefabs/Maps/SearchExtract_Demo_01.prefab`：自 `PushMap_Demo_01` 复制，**未改** PushMap 原图
- `Objective_01` 保留源点位（半径 0.79）；新增 `Objective_02`（`(9.51, 4.95)`，半径 **2**）；`SP_01`/`SP_02` 对齐刷怪表；副本内将落入 `AirWall` OBB 的 `SP_02` / `BossPoint` 挪出
- 玩法表样例 `SearchExtract_01.MapId=SearchExtract_Demo_01`（Mode2 Excel+CSV）；`MapPrefabPaths` / 加载器合法池增 `SearchExtract_*`
- Editor：`Gravedigger2026/SearchExtract/Ensure Sample Map Prefab` + `Validate Sample Map AirWalls`；Catalog 已绑 `SearchExtract_Demo_01`
- StageModule **未做**（SE-03）
