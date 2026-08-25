---
title: 在 Ground_* / PushMap_* 上铺 Water 与 Foam 层
status: in-progress
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_04 §13 地图 Prefab / Isometric Tilemap
  - SPEC_04 §2 禁止运行时引用 SmallScaleInt
  - .scratch/map-flowing-water/issues/01-port-builtin-shaders.md
depends_on:
  - MW-01
selected_approach: A — 示例同款两层；材质用 Art/Maps 落盘资产
---

## 目标

在运行时地图 Prefab 上做出示例那种「整片连续流动 + 岸边泡沫」。依赖 MW-01 材质已可用。

## 范围

- `Assets/Prefabs/Maps/Ground_01`…`Ground_05`（Dig / Defend 共用）
- `Assets/Prefabs/Maps/PushMap_Demo_*`（及已提交的其它 `PushMap_*`）

## 实现（本会话）

- Editor：`Assets/Editor/Maps/MapFlowingWaterBuilder.cs`
- 同 Grid `GroundTilemap` 下 Ensure `Water`（Chunk + `Water.mat`，Order=-4）与 `Foam`（Individual + `Foam.mat`，Order=-3）
- Demo 池：曼哈顿半径 2、原点 `(-4,3)`；Water mask=`Art/Maps/Tiles/BLACK TILE`；Foam=`WaterRipples 1`～`13`
- 不改 `WalkSurface` / NavMesh / AirWall / EngageZone
- 菜单：`Gravedigger2026/Maps/Ensure Flowing Water Layers (preserve paint | force Demo pond)`
- 打开工程 one-shot（preserve）；本机无 Unity CLI 时需负责人打开工程或跑菜单写入 Prefab

## 步骤（可粘贴）

1. 各图 Grid 下新增 Tilemap `Water`、`Foam`（与现有地面层同 Grid / CellSize）。
2. `Water`：`TilemapRenderer` 材质 = `Art/Maps` 的 `Water.mat`；Mode=Chunk；Sorting Order 低于地面装饰、低于 Foam。
3. `Foam`：材质 = `Foam.mat`；**Mode=Individual**；Order 高于 Water。
4. Water 层用水面形状砖（alpha 当遮罩即可，不必是蓝色图）。
5. Foam 层在同一水域刷 `Art/Maps/Environment/Animated tiles/WaterRipples 1`～`13`。
6. 手验 Dig / Defend / PushMap Instantiate 后水面流动、无粉红、无 `SmallScaleInt` 引用。
7. 水面砖不改变 `WalkSurface` / NavMesh / AirWall。

## 验收

- [ ] 目标 Prefab 均有 Water/Foam 层且引用 Art/Maps 材质
- [ ] Play 模式连续流动可见；Foam 仅岸边/涟漪轮廓有白边
- [ ] 寻路与占领逻辑不变
