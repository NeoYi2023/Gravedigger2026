---
title: PushMap — BOSS 通关与奖励钩子
status: done
difficulty: 2
demo_scope: authorized
approach: A
spec_refs:
  - SPEC_03 §3.14 BOSS 与胜负 / 经验
  - SPEC_04 §9.22 StageExpReward / CaptureLoot / DungeonUnlockIds
---

## 目标

击杀 BossPoint 怪物 → Ended → 入账 StageExpReward → TryAdvanceStage；Shield≤0 → LevelFailure；占领可发 CaptureLoot + DungeonUnlock 存档钩子（副本玩法不做）。

## 范围

- IsBoss 行 / BossPoint 一致性校验或约定
- 经验路径对齐 Defend

## 不做

- 副本玩法正文与 UI
- 完整失败结算 UI

## 验收

- [x] BOSS 击杀推进阶段并可观察 Exp
- [x] 护盾归零不入账并中止关卡
- [x] 解锁 ID 写入存档集合可日志验证

## 依赖

- [05](05-spawn-trap.md)

## 编码前

- 难度 2 方案比选；须 Demo 授权

## 本会话交付（方案 A）

- SPEC：SPEC_03 §3.14 增「Demo BOSS 通关边界」+ 胜负规则归属（中英）；SPEC_04 §6 增 PM-07 段、§9.22 增「BOSS 通关与奖励运行时契约」（中英）；SPEC_00 v0.60.0
- 规则：`PushMapSessionService` 增 `_pendingBossCount` / `TryNotifyBossKilled` / `VictorySettled(StageExpReward)` / `NotifyBossPointPresence`；LevelFailure 明确不发 VictorySettled
- 存档钩子：`DungeonUnlockService`（PlayerPrefs 按槽 `HashSet` + `[DungeonUnlock]` 日志）
- 表现：`PushMapMonsterAgentView.IsBoss`/`MarkAsBoss`；`PushMapStageController` Demo 击杀＝忠诚兵进 BOSS `AttackRange` → `NotifyKilled`+`TryNotifyBossKilled`；`VictorySettled`→`AddExperience`→`_onVictoryAdvance`；占领入账 `CaptureLoot`；通关/占领写 `DungeonUnlockIds`
- 接线：`MetaShell` 进档 `BindSlot`；`PushMapStageModule` 注入 `DungeonUnlockService`
- 样例可验：`BossPoint` 挪近 Obj2；`Monster_10` AlertRadius=6 / AttackRange=1.5（便于 Demo 接触）
- 边界：副本玩法正文 / 完整失败 UI 不做；普通怪真实士兵伤害仍后置
- 工程：`dotnet build Assembly-CSharp.csproj` 0 错误
