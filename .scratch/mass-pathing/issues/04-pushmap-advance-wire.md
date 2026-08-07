---
title: MassCombatPathing — PushMap 推进接线（FlowField）
status: done
difficulty: 3
demo_scope: authorized
spec_refs:
  - SPEC_03 §3.14 士兵推进 / FlowField 重建 / Demo 遇敌暂停
  - SPEC_04 §9.7 / §9.22 PushMap 推进
  - SPEC_04 §6 MassCombatPathing
approach: B
---

## 目标

PushMap 忠诚兵推进从「每人 NavMesh SetDestination 到 Objective」改为：**采样共享 FlowField + LocalDetour**；`CurrentObjective` 切换 / 开战 Bake（含 AirWall）后重建场。

## 范围

- `MassMoveScheduler` 分帧骨架（本片可只服务推进单位）
- `PushMapStageController`：Bake 后建可走掩码 → `FlowFieldService.Rebuild`
- 订阅 `CurrentObjectiveChanged` → Rebuild
- 改造或包裹 `PushMapAdvanceView`：跟场方向移动；遇敌气泡暂停时停跟场（保留既有暂停语义）
- Rebel 仍不推进

## 不做

- AttackSlot 追击接线（→ MP-05）
- Defend 接线（→ MP-06）
- 200 人压测（→ MP-07）

## 验收

- [x] 多忠诚兵共当前目标：共享一场，日志/调试可见单次 Rebuild
- [x] 空气墙不可穿（场障碍生效）
- [x] 占领切下一目标后场重建，兵改向新目标
- [x] Demo 遇敌暂停仍可验；恢复后继续跟场
- [x] 不再对每兵每帧 `SetDestination(Objective)`

## 依赖

- [01](01-flow-field-core.md)
- [03](03-local-detour-spatial-hash.md)
- PushMap PM-04 / PM-08 已落地

## 实现摘要（MP-04）

- Core：`StaticBoxWalkableMask`（AirWall OBB）、`MassMoveScheduler`（≤50 steer/帧 + SpatialHash + LocalDetour）
- `PushMapStageController`：Bake 后 Configure 掩码+场；`CurrentObjectiveChanged` → `Rebuild`（日志含 RebuildCount）；Combat `Tick` Scheduler
- `PushMapAdvanceView`：`NavMeshAgent.Move` 跟场；无 `SetDestination(Objective)`；遇敌气泡 `SetPaused`；Rebel 跳过
- SPEC：v0.73.3 — §9.22 推进契约对齐方案 B

## 手验

1. PushMap 开战：Console 见 `FlowField configured` + `FlowField Rebuild shared field … RebuildCount=1`
2. 多忠诚兵同向当前 Objective，无每人 SetDestination
3. 绕开/不可穿 AirWall；占领切目标后再见一次 Rebuild，兵改向
4. 贴近存活怪暂停，离开后继续跟场
