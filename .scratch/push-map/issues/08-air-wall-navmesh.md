---
title: PushMap — 空气墙阻挡与 NavMesh
status: todo
difficulty: 2
demo_scope: deferred
spec_refs:
  - SPEC_03 §3.14 AirWall
  - SPEC_04 §13 地图标记
---

## 目标

空气墙阻挡敌我双方寻路/进入；支持 45° 旋转；与 Runtime NavMesh 或等价阻挡一致。

## 范围

- Prefab AirWall → NavMeshObstacle 或烘焙洞
- 敌我士兵与怪物均不可穿

## 不做

- 复杂多层障碍 polish

## 验收

- [ ] 单位无法穿过空气墙（含 45° 实例）

## 依赖

- [01](01-map-prefab-markers.md)
- [03](03-stage-module-wire.md)

## 编码前

- 可与 PM-04～05 并行；难度 2；须 Demo 授权
