# SC-01 — SoftCollisionService 核心

**状态：** todo  
**难度：** 3  
**依赖：** SC-00；复用 MP-03 `SpatialHash2D`

## 目标

纯 C# `SoftCollisionService`：登记/注销足迹，分帧邻域排斥，写出 `CorrectionXz`。

## 范围

- `Register` / `Unregister` / `Tick(dt, maxBodiesPerFrame)`
- 半径 = `BodyRadius` 或士兵 Demo 半径
- `repulsionScale`；交战可降（调用方传入）
- `ResolveCollisions` 开关（默认 true）
- 正确性单测或 `*CorrectnessChecks`（重叠→分离；关 Resolve 可重叠；无 O(n²)）

## 不做

- Stage/View 接线（→ SC-03）
- Surround 槽位（→ SC-02）
- Follow 任何逻辑

## 验收

- 单元可测；热路径复用列表；预算可与 MassMoveScheduler 对齐说明写在注释
