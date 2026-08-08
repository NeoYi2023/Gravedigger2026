---
title: SoftCollision 核心服务（并肩不穿模）
status: done
difficulty: 3
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12 方案 B+ SoftCollision
  - SPEC_04 §9.7 B+ 运行时契约
---

## 目标

实现纯 C# `SoftCollisionService`：集中登记可移动单位足迹，分帧邻域排斥，写出 `CorrectionXz`；供 `MassMoveScheduler` 叠加在 LocalDetour 之后。

## 范围

- 新文件：`Assets/Scripts/Core/Pathing/SoftCollisionService.cs`
- API：`Register(id, radius, getPos) / Unregister(id) / Tick(dt, maxBodiesPerFrame) / TryGetCorrection(id, out Vector2 correctionXz)`
- 半径：怪物用 `MonsterConfig.BodyRadius`；士兵 Demo 半径与 `NavMeshAgent` 对齐（0.1）
- 复用 `SpatialHash2D`；禁止 O(n²)
- `repulsionScale` 可调；`ResolveCollisions` 开关默认 true
- 完全重叠时按稳定 ID 确定性侧推，避免零向量死锁
- 正确性自检（重叠→分离；关 Resolve 可重叠）

## 不做

- Stage / View 接线（→ SC-03）
- Surround 槽位（→ SC-02）
- Follow / 军队粘随
- 替代 NavMesh / AirWall 静态阻挡

## 验收

- [x] 单元可测；热路径无每帧分配（`SoftCollisionCorrectnessChecks.RunAll`；池化 SpatialHash2D + 复用缓冲）
- [x] 与 `MassMoveScheduler` 分帧预算对齐（`DefaultMaxBodiesPerFrame = MaxRecalcPerFrame = 50`，轮转保留）
- [x] 关闭 Resolve 后对照可重叠（`CheckResolveOffAllowsOverlap`）

## 落地记录

- 选定方案：**A — 位置冲量 + 轮转保留**（难度 3 已确认）
- 新建：`Assets/Scripts/Core/Pathing/SoftCollisionService.cs`、`SoftCollisionCorrectnessChecks.cs`
- 冲量上限 `MaxCorrectionSpeed = 2.0` u/s（实现常量，防爆冲瞬移）；未轮到帧保留上次 `CorrectionXz`
- SPEC_04 §9.7 触点签名已澄清；SPEC_00 Changelog v0.74.3

## 依赖

- 方案 B / MP-03 `SpatialHash2D`
- [00](00-spec-close.md)
