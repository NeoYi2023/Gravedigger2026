---
title: TF-04b PushMap/Defend Stage 入阵分流 + AttackSlot leash
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.18 战斗移动
  - SPEC_04 §9.7 战术阵型运行时契约
  - SPEC_03 §3.12 MassCombatPathing
approach: A
depends_on:
  - TF-04
---

## 目标

StartBattle 从 Layout 会话锁 RuntimeService；PushMap 中心跟 FlowField、成员 FormationSlot；Defend 守点；接敌 AttackSlot leash 钳制。

## 范围

- PushMap：`PushMapAdvanceView` / StageController 分流（入阵 vs 未入阵）
- Defend：`WarriorAgentView` / `DefendStageController` 对等
- StartBattle：`OnStartBattle(layout, configs, patterns, centerMode, representativeMoveSpeed)`
- 每帧：`Tick` + `SetGoal(FormationSlot, slotWorld)`；接敌 `ClampToLeash`；超 leash 不追
- `KeepFormationWhileEngage`：无目标时是否仍趋近槽位

## 不做

- Stat/专属技能 overlay（TF-05）
- 解散 / Rebel 退出（TF-05）

## 验收

- [x] PushMap：入阵成员保持相对站位推进；未入阵仍跟场
- [x] Defend：整阵守组阵点；接敌不无限追击超 leash 目标
- [x] SoftCollision / LocalDetour 仍作用于成员

## 依赖

- TF-04a

## 落地摘要

选定方案 A（难度 2）：共享 `TacticalFormationCombatGoalPolicy`；Stage 拥有 SetGoal。开战在 `CloseFormationEditor` 前拷贝 CombatLock，部署后 `OnStartBattle`。PushMap 中心每帧按虚拟中心采样 FlowField 积分；Defend `Hold`。入阵空闲 `FormationSlot`（`KeepFormationWhileEngage=0` 则回 Objective/Home）；接敌 AttackSlot 目的地 leash 投影；敌人中心超 leash 不追（已进距可停步挥刀）。成员已处 `FormationSlot` 时每帧刷新槽位世界点。Rebel/未入阵不走本分流。Correctness 增补 Policy 用例（同 TF-04a 菜单）。
