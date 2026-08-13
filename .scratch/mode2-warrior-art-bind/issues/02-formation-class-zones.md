---
title: Ground_* 补齐 Mode2 全部职业 FormationClassZone
status: done
difficulty: 1
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.8 D-057
  - SPEC_03 §3.15 自动上阵
  - SPEC_04 §13 FormationClassZone
  - SPEC_00 v0.80.6
---

## 目标

暗黑法师等新职业自动上阵能落入职业区，不再因「No FormationClassZone」留池。

## 范围

- 扩展 `DefendAssetBuilder.EnsureFormationClassZones`：在既有 11 区之外增加 8 区（坐标相对地图中心；Y=25°；HalfExtents 与现区相同 Demo `0.45,0.35`）
  - 第二前排 z=−1.9：`Class_Guardian` (−2.0)、`Class_Brawler` (0.0)、`Class_Shadowblade` (2.0)
  - 第二后排 z=+1.7：`Class_Longbowman` (−2.0)、`Class_BombMaster` (−1.0)、`Class_IceMage` (0.0)、`Class_FireMage` (1.0)、`Class_DarkMage` (2.0)
- 对 `Ground_01`…`Ground_05` 跑 Ensure（`EnsureEngageZonesAndSpawnPointsOnMaps` 或等价只写区）；**禁止** `GenerateAll`
- 既有 11 区坐标不变

## 不做

- Prefab/Catalog 外观（WA-01）
- 改螺旋算法 / PlacementOrder 表
- PushMap 专用地图职业区（AutoManufacture 用下一关 `Ground_*`）

## 验收

- [x] 各 `Ground_*` 含 `Class_DarkMage` 等 8 个新区；`AutoFormationDeploy` 不再对该 ClassId 打 No FormationClassZone
- [x] Mode2 挖出暗黑法师手臂后自动上阵落在后排新区（区在 z=+1.7；手验进 Play）
- [x] 圣骑士仍走既有 `Class_Paladin` 区（坐标未改）

## 依赖

- WA-01（模型先可见，再验上阵位置）

## 编码前

- 难度 1；布局已锁在 SPEC_04 §13；可直接编码
