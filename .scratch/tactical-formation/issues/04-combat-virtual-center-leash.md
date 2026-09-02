---
title: TF-04a 虚拟中心纯数据 + FormationSlot + leash 核心
status: done
difficulty: 3
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.18 战斗移动
  - SPEC_04 §9.7 战术阵型运行时契约
  - SPEC_03 §3.12 MassCombatPathing
approach: A
depends_on:
  - TF-03
---

## 目标

开战锁定快照；虚拟中心由 `TacticalFormationRuntimeService` 纯数据持有（不进 SoftCollision）；成员 `GoalKind.FormationSlot` 直趋槽位世界点；leash 投影为纯函数。

**选定方案：** A — Runtime 中心纯数据。Stage 接线见 [TF-04b](04b-stage-pushmap-defend-wire.md)。

## 范围（本片）

- `GoalKind.FormationSlot` 枚举 + `MassMoveScheduler` 直趋解析（同 FormationHome）
- `TacticalFormationRuntimeService` StartBattle 快照、中心积分/守点、槽位世界点、`ClampToLeash`
- `WarriorTaskDebugLabelView` 增「阵型」简标
- scene-free Correctness + Editor 菜单

## 不做

- PushMap / Defend Stage 开战注册与入阵分流（TF-04b）
- Stat/专属技能 overlay（TF-05）
- 第二种阵型多实例（Demo 每种 FormationId 最多 1 实例）

## 验收

- [x] `FormationSlot` 直趋 DesiredDestination；SoftCollision / LocalDetour 仍作用于该 agent
- [x] 槽位世界点 = center + Rot(facing) * slotLocal（与 Prepare snap 同旋转）
- [x] Hold：中心不移；FollowFlowField：沿 dir 积分
- [x] AttackSlot 点超 leash 投影到圆周；圈内不变
- [ ] Correctness 菜单全绿（菜单 `Gravedigger2026/Formation/Run Tactical Formation Runtime Correctness (TF-04a)`；请在 Editor 跑一次确认）
- [x] Debug 简标 `FormationSlot`→「阵型」

## 依赖

- TF-03

## 落地摘要

选定方案 A；难度 3；本会话只做 TF-04a。虚拟中心不进 `MassMoveScheduler`（无 SoftCollision 幽灵体）。`TacticalFormationRuntimeService.OnStartBattle` 锁定槽位本地偏移 + 移动参数 + 中心/朝向；`Tick` 在 `FollowFlowField` 沿 dir 积分（`MoveSpeed × CenterMoveSpeedMul`）并按 `FacingTurnRate` 转向；`Hold` 守点。`GoalKind.FormationSlot` 由 Scheduler 直趋 `DesiredDestination`。`ClampToLeash` 投影超半径点。Stage 接线 → TF-04b。
