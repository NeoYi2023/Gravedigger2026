---
title: PushMap — 地图 Prefab 标记契约与样例
status: todo
difficulty: 2
demo_scope: deferred
spec_refs:
  - SPEC_03 §3.14 地图 Prefab 标记
  - SPEC_04 §9.22 地图 Prefab 标记契约
  - SPEC_04 §13 Prefabs/Maps
---

## 目标

在 `Assets/Prefabs/Maps/` 落地（或样例）PushMap 地图 Prefab 标记：Objective / CaptureZone / AirWall(45°) / SpawnPoint / TrapZone / BossPoint；地面可用 Tile Palette。

## 范围

- MapId ∈ `Ground_*` 或 `PushMap_*`
- CaptureZone 默认半径 2（SerializeField 可改）
- AirWall 支持 Y 轴 45° 旋转
- EngageZone / WalkSurface 复用现有约定

## 不做

- 运行时占领/刷怪逻辑（PM-04/05）
- 正式美术 polish

## 验收

- [ ] 样例地图 Prefab 含全部标记类型且可 Instantiate
- [ ] 空气墙 45° 旋转在编辑器可设

## 依赖

- [00](00-spec-close.md)

## 编码前

- 工作量可单次；难度 2 → 方案比选后再编码
- 须负责人授权 PushMap Demo
