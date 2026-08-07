---
title: MassCombatPathing — FlowField 核心（共享目标）
status: done
difficulty: 3
demo_scope: authorized
spec_refs:
  - SPEC_03 §3.12 MassCombatPathing / FlowField
  - SPEC_03 §3.14 FlowField 重建 / 士兵推进
  - SPEC_04 §9.7 FlowField 运行时契约
approach: B
---

## 目标

实现纯 C# `FlowFieldService`：对共享世界目标构建方向场；同目标单位采样同一缓冲；静态不可走（含 AirWall 掩码）写入障碍。

## 范围

- `Assets/Scripts/Core/Pathing/FlowFieldService.cs`（及所需小型辅助）
- 格点覆盖 IsoDiamond / `DigMapBounds`；Demo cell **0.25～0.5**
- API：`Rebuild(goal, walkableMask)` / `SampleDir(worldPos) -> Vector2`
- 单元可测：无 Unity 场景依赖的场正确性（可达格指向目标、障碍格不可达）
- 与现有 Bake 结果对接的**接口约定**（掩码来源可先 stub；正式接线在 MP-04）

## 不做

- 接入 `PushMapAdvanceView`（→ MP-04）
- AttackSlot / LocalDetour
- 每单位 A* / `NavMesh.CalculatePath` 批量

## 验收

- [x] 单目标 Rebuild 后，从多点 SampleDir 最终可汇聚到目标邻域
- [x] 障碍格不产生穿墙方向
- [x] 同目标二次 Sample 不触发第二次全图搜索（共享缓冲）
- [x] 无友军动态障碍写入场

## 依赖

- [00](00-spec-close.md)

## 实现摘要（MP-01）

- `IFlowFieldWalkableMask` + `StubFullyWalkableMask`（Bake/AirWall → MP-04）
- `FlowFieldService`：`Configure` IsoDiamond 覆盖 → `Rebuild` Dijkstra 积分场 → `SampleDir` 只读缓冲；对角禁角切
- `FlowFieldCorrectnessChecks.RunAll()`：无场景自检（汇聚 / 障碍 / RebuildCount / API 无友军列表）
