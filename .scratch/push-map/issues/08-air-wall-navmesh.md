---
title: PushMap — 空气墙阻挡与 NavMesh
status: done
difficulty: 2
demo_scope: authorized
spec_refs:
  - SPEC_03 §3.14 AirWall
  - SPEC_04 §9.22 空气墙 NavMesh 契约
  - SPEC_04 §9.7 / §13 Prefabs/Maps
approach: A
---

## 目标

空气墙阻挡敌我双方寻路/进入；支持 45° 旋转；与 Runtime NavMesh 或等价阻挡一致。

## 范围

- Prefab AirWall → NavMeshObstacle 或烘焙洞
- 敌我士兵与怪物均不可穿

## 不做

- 复杂多层障碍 polish

## 验收

- [x] 单位无法穿过空气墙（含 45° 实例）

## 依赖

- [01](01-map-prefab-markers.md)
- [03](03-stage-module-wire.md)

## 编码前

- 可与 PM-04～05 并行；难度 2；须 Demo 授权

## 本会话交付（方案 A）

- SPEC_00 v0.61.0 + SPEC_03 §3.14 Demo 空气墙边界 + SPEC_04 §6/§9.7/§9.22/§13：开战 Bake 注入 `NavMeshBuildSource` Box + Not Walkable
- `DefendNavMeshBaker.Bake(..., notWalkableBoxes)` + `NavMeshBoxObstacle`
- `PushMapStageController` StartBattle 收集 `AirWall`（含样例 `AirWall_45` Y=45°）传入 Bake
- `AirWall.FullSize`（`HalfExtents×2`）供 Box size
