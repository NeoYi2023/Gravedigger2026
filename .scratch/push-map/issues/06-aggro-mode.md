---
title: PushMap — AggroMode 四态 AI
status: done
difficulty: 3
demo_scope: authorized
approach: A
spec_refs:
  - SPEC_03 §3.14 AggroMode
  - SPEC_04 §9.19 AggroMode / AlertRadius
---

## 目标

在 `MonsterAgentView`（或规则层）实现 ActiveChase / PassiveChase / StationaryActive / StationaryPassive；AttackMode 仍走命中方案 D。

## 范围

- AlertRadius 与 AttackRange 并列
- 仅对忠诚士兵触发主动发现（按 SPEC）

## 不做

- 技能施放
- 副本玩法

## 验收

- [x] 四态各至少 1 只样例怪可区分行为（`Monster_01`=ActiveChase / `Monster_02`=PassiveChase / `Monster_03`=StationaryActive / `Monster_04`=StationaryPassive；均由 `PushMap_01` 刷怪行刷出）

## 依赖

- [05](05-spawn-trap.md)

## 编码前

- 难度 3 方案比选；可与 PM-07 部分并行；须 Demo 授权

## 本会话交付（方案 A）

- SPEC：SPEC_03 §3.14 增「Demo AggroMode 边界」（中英）、SPEC_04 §6 增 PM-06 段、§9.23 增「AggroMode 运行时契约」（中英）；SPEC_00 v0.59.0
- 表现：`Assets/Scripts/Gameplay/PushMap/PushMapMonsterAgentView.cs` 按 `config.AggroMode` 分支——`ActiveChase` 忠诚兵进 `AlertRadius` 追击；`PassiveChase` 未挑衅静止、`NotifyProvoked()` 后追击；`StationaryActive` 永不移动、`AttackRange` 内攻击；`StationaryPassive` 永不移动、须挑衅且目标仍在 `AttackRange`。检测/挑衅仅对忠诚兵（`!IsRebel`）
- 挑衅接线：`PushMapStageController.PollPassiveProvocation`（忠诚 `PushMapAdvanceView` 首次进入被动怪 `AttackRange` → `NotifyProvoked()`，等效「士兵先攻击」；士兵 HP 后置）；`IsPassive`/`IsStationary`/`AttackRange` 公开只读
- 边界：命中仍 `AttackMode` 方案 D；主动态不进 `AlertRadius` 主动发现主角；士兵击杀怪物 / 技能施放 / 副本玩法不做（PM-07+）
- 工程：`dotnet build Assembly-CSharp.csproj` 0 错误
