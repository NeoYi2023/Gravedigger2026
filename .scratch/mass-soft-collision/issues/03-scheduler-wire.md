---
title: MassMoveScheduler / Stage 接线（B+ 全链）
status: done
difficulty: 3
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12 方案 B+
  - SPEC_03 §3.14 遇敌 / 到达守备
  - SPEC_04 §9.7 B+ 运行时契约
---

## 目标

把 SC-01 / SC-02 接入运行栈：`MassMoveScheduler.Tick` 最终位移 = `desiredDir → LocalDetour → + SoftCollision.Correction → View.Move`；近战多打一认领带 Surround。

## 范围

- PushMap / Defend 的 StageController 开战注册、死亡注销 SoftCollision
- 交战状态（`GoalKind=AttackSlot`）`repulsionScale` 降至约 0.35～0.5
- 继续保持 `NavMeshAgent` ObstacleAvoidance 关闭（既有约定）
- CombatMoveMode 推导：AttackSlot + 近战 → Surround；其余 → Chase；Objective / FormationHome 无 Follow
- View 仍只应用位移，不改规则层接口语义

## 不做

- Sweep 技能波
- 改 FlowField 语义
- 引入 Follow 模式

## 验收

- [ ] 手玩：多近战围怪可见环上缺口；单位重叠明显减轻（待负责人在 Unity 场景确认）
- [x] 无「全队粘随主角」回归（未引入任何 Follow 路径；Objective/FormationHome 推导=Chase，FlowField 语义零改动；自检含推导表）
- [x] CaptureZone 到达后守备语义不变（`ObjectiveArriveRadius` hold 逻辑未动；守备帧 steer=0 时 correction 仍叠加，分离增强而非语义改写；自检 `ZeroSteerHoldStillSeparates`）
- [x] 自动化：注册/注销同步、重叠产 correction、交战降强度、Resolve 关可重叠、近战推导 Surround（`SoftCollisionWireCorrectnessChecks.RunAll` 六检；脱机 mono 编译运行全过，SC-01/SC-02 既有自检同跑无回归）

## 落地记录

- 选定方案：**A — Scheduler 组合 SoftCollision + View 叠加 correction**（难度 3 已确认）
- 新建：`Assets/Scripts/Core/Pathing/CombatMoveMode.cs`（枚举 Chase/Surround/Sweep 无 Follow + `CombatMoveModePolicy.Derive` / `SurroundFor`：AttackSlot/ChaseAnchor+Melee→Surround(`SurroundParams.Default`)，其余→Chase；Sweep 保留不接线）；`SoftCollisionWireCorrectnessChecks.cs`（六检）
- 改动：
  - `MassMoveScheduler.cs` — 内聚持有 `SoftCollisionService`（`SoftCollision` 只读属性暴露 `ResolveCollisions` 调试开关）；`Register/Unregister/Clear` 自动同步 soft body（`getPos` 按 id 回读样本位，开战注册/死亡注销复用既有 View Bind/OnDisable 路径，零新增接线点）；`Tick(samples, dt)` 增 dt 形参（帧序=ApplySamples→哈希→SoftCollision.Tick→steer 轮转）；`SetGoal` 按 GoalKind 同步单体 `repulsionScale`（`AttackSlotRepulsionScale=0.35`）；新增 `TryGetCorrection` 转发
  - `SoftCollisionService.cs` — `SetRepulsionScale(id, scale)` / `TryGetRepulsionScale` 单体 API（默认 1.0；有效强度=全局×单体）
  - 4 View（`WarriorAgentView` / `MonsterAgentView` / `PushMapAdvanceView` / `PushMapMonsterAgentView`）— `Move` 处 `delta = steer·speed·dt + correctionXz`；**steer 与 correction 皆零才早退**（守备/攻击暂停帧分离不丢）
  - 5 处 `TryClaim`（Defend×3 / PushMap×2）按 `CombatMoveModePolicy.SurroundFor` 传参；2 处 Stage `Tick` 传 `Time.deltaTime`；压测 `MassPathingPerfStress` / `MassPathingPerfStressView` 编译适配（Tick 签名）
  - `MassPathingPerfStressMenu.cs` +「Run SoftCollision Wire Correctness Checks (SC-03)」菜单
- ObstacleAvoidance：4 View 现状即 `NoObstacleAvoidance`，保持不变（未改）
- SPEC_04 §9.7 B+ 契约补 SC-03 落地澄清（中英）；SPEC_00 Changelog v0.74.5
- 脱机验证：`.scratch/mass-soft-collision/verify-sc03/`（Unity Mono mcs；`??=` 仅验证副本改写）；SC-01 + SC-02 + SC-03 自检全过
- 压测含 SoftCollision 的 200v200 回归判定 → SC-04

## 依赖

- [01](01-soft-collision-core.md)
- [02](02-surround-gap-slots.md)
