---
title: PushMap — 怪物占地散开（BodyRadius + Agent 避障）
status: done
difficulty: 2
demo_scope: authorized
spec_refs:
  - SPEC_03 §3.14 占地散开 / BodyRadius / 持续避让
  - SPEC_04 §9.19 MonsterConfig.BodyRadius
  - SPEC_04 §9.23 PM-10 刷出散开 + NavMeshAgent.radius
approach: A
---

## 目标

同点多只 / 邻近已刷怪按 `BodyRadius` 错开落点；Combat 中移动怪用 `NavMeshAgent.radius = BodyRadius` 持续互避，减少互相遮挡。

## 范围

- `MonsterConfig.BodyRadius`（缺省 `0.35`；`<0` 加载失败）
- `PushMapSpawnSpread`：环形/螺旋 + 避场上存活怪 + `NavMesh.SamplePosition`
- `PushMapStageController.HandlePushMapSpawnRequested` 接线
- `PushMapMonsterAgentView.Bind`：`agent.radius = BodyRadius`

## 不做

- 物理 CapsuleCollider / Rigidbody 互推
- 士兵 Agent 半径 / 怪–兵防叠
- Defend `MonsterAgentView` 落点散开
- 自定义每帧分离力

## 验收

- [x] `SpawnCount≥3` 同点开战不重叠
- [x] 陷阱再刷不穿进已刷堆（按存活占地圆避让）
- [x] 追击时移动怪 `NavMeshAgent.radius` 为 BodyRadius
- [x] `Stationary*` 刷出已散开且站桩不漂移
- [x] `BodyRadius` 空列加载为 `0.35`

## 依赖

- [05](05-spawn-trap.md)

## 本会话交付（方案 A）

- SPEC_00 v0.69.0 + SPEC_03 §3.14 + SPEC_04 §6/§9.19/§9.23 + CONTEXT
- Excel/CSV `BodyRadius`；`MonsterConfigRow` / `ConfigCsvRepository`
- `PushMapSpawnSpread.cs` + StageController / MonsterAgentView 接线
