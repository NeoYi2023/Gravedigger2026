---
title: FormationClassZone 运行时 IsoDiamond mesh 与 Contains
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.8 D-052
  - SPEC_03 §3.15 自动上阵
  - SPEC_04 §13 FormationClassZone
  - SPEC_00 v0.82.15
selected_approach: A — WalkSurface 同形 IsoDiamond
---

## 目标

职业区 Contains / 螺旋 / Scene 网格与 WalkSurface 同形：无 Y 旋转的 XZ IsoDiamond。

## 范围

- `FormationClassZone`：对齐 `WalkSurfaceIsoDiamond.RebuildMesh`；Require MeshFilter/MeshCollider；Play 关闭 Collider
- `FormationClassZonesRoot`：废止 IsoTileYaw；子区画菱形 Gizmo；identity 作者框
- `FormationClassZoneSnapshot` / Collector：去掉 `RotationYDegrees`
- `FormationZoneSpiralSearch`：菱形 Contains + `|dx|/(hx-r)+|dz|/(hz-r)≤1` 内缩
- `MapFootprintMath`：小尺寸重载（minExtent ≈ 0.05）；禁止职业区走 Sanitize 下限 0.5
- 禁止把职业区网格交给 `DefendNavMeshBaker`

## 不做

- Ensure 写回 Prefab（FZ-02）
- 改 PlacementOrder / 区中心布局 / HalfExtents 数值
- 抽公共 IsoDiamondVolume

## 验收

- [x] `ContainsXZ` 与 WalkSurface 同公式
- [x] 螺旋不再用 OBB/`RotationYDegrees`
- [x] Play 模式 MeshCollider.enabled=false
- [x] 小 HalfExtents（如 0.45）不被撑到 0.5

## 依赖

- FZ-00

## 编码前

- 整包难度 2、方案 A 已选定；本片按 SPEC 直接编码
