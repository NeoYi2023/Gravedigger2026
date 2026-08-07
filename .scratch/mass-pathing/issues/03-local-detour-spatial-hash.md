---
title: MassCombatPathing — SpatialHash + LocalDetour
status: done
difficulty: 3
demo_scope: authorized
spec_refs:
  - SPEC_03 §3.12 LocalDetour
  - SPEC_04 §9.7 LocalDetour / 性能预算
approach: B
---

## 目标

实现 `SpatialHash2D` + `LocalDetourSolver`：默认沿期望方向直线；前方友军阻挡时左/右短探测选一侧绕行；禁止友军 Carve。

## 范围

- `SpatialHash2D`：插入/查询邻域；cell ≈ 0.5
- `LocalDetourSolver.Steer(desiredDir, self, neighbors) -> steerDir`
- 前方扇形 + 左右探测长度 ≈ 1.0
- 可选软分离（低强度）；交战圈可降分离的开关参数
- 无分配热路径（复用列表缓冲）

## 不做

- 完整 ORCA
- NavMeshObstacle
- Stage 接线（→ MP-04/05）

## 验收

- [x] 无邻域时 steer ≈ desired
- [x] 正前方友军时选择左或右偏置（可测断言）
- [x] 邻域查询不扫描全表（哈希桶）
- [x] Update 热路径无 GC 尖峰（Profiler 手验可接受）— API 复用 `List` 缓冲；`LocalDetourCorrectnessChecks.CheckHotPathReusesNeighborList` 结构保证；Stage 接线后 Profiler 再确认

## 依赖

- [00](00-spec-close.md)

## 交付摘要（本切片）

- Core：`SpatialHash2D.cs`、`LocalDetourSolver.cs`、`LocalDetourCorrectnessChecks.cs`
- SPEC：v0.73.2 — `Steer(desiredDir, self, neighbors, separationScale?)` 触点签名
- 手验：`LocalDetourCorrectnessChecks.RunAll()` → null / PASS
