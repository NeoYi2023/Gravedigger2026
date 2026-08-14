---
title: Ensure 废止 IsoTileYaw 并写回 Ground_*
status: done
difficulty: 1
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.8 D-052
  - SPEC_03 §3.8 D-057
  - SPEC_04 §13 FormationClassZone
  - SPEC_00 v0.82.15
selected_approach: A — WalkSurface 同形 IsoDiamond
---

## 目标

样例 `Ground_01`…`Ground_05` 的 `FormationClassZones` 父/子 identity，并补 Mesh 组件，使 Scene 菱形与 GroundTilemap / WalkSurface 同朝向。

## 范围

- `DefendAssetBuilder.EnsureFormationClassZones`：删除 `ApplyIsoAuthoringFrame`；父/子 identity；为已有区补 MeshFilter/MeshCollider/MeshRenderer 并 Rebuild
- 菜单「Ensure Formation Class Zones on Maps」写回五张图
- **不覆盖** 世界坐标与 HalfExtents
- **禁止** `GenerateAll`

## 不做

- 改螺旋 / PlacementOrder
- PushMap 专用地图职业区

## 验收

- [x] 各 `Ground_*` 的 `FormationClassZones` Y=0（非 -26.57°）
- [x] Ensure 为已有区补 Mesh 并 Rebuild（打开 Prefab / 跑菜单即写入组件）
- [x] 未调用 GenerateAll

## 依赖

- FZ-01

## 编码前

- 难度 1；Ensure 契约已锁在 SPEC_04 §13
