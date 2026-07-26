---
title: Defend — 士兵 EngageZone 近战普攻与清场可检测（方案 A）
status: done
difficulty: 3
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12 WarriorCombat / 命中方案 D（近战） / 战斗派生公式
  - SPEC_04 §9.7 EngageZone
  - SPEC_04 §9.9b ClassConfig
  - SPEC_04 §9.19 MonsterConfig
---

## 目标

非叛变 **近战** 士兵仅在 EngageZone 内选最近怪；按 ClassConfig 前摇命中；Demo 仅普攻。清场条件可检测（不入账、不切胜利 Ended——见 05d）。

## 范围

- NormalAttackPower / AttackSpeed 派生（ClassConfig + CombatConvertCoeffs）
- 命中方案 D **近战**分支（`AttackMode=Melee`）
- 怪物对士兵：AttackPower 直接扣 HP（无护甲）
- CombatDead 最小行为（停手即可；宝石 PermanentDeath 本片仅停手+标记）
- 清场可检测：刷怪行全触发 + 已刷怪全灭 → 事件/日志（**不**做经验入账）

## 不做

- 远程弹道（`AttackMode=Ranged`）→ [05c2](05c2-defend-ranged-projectile.md)
- 技能施放、复活技能、清场胜利结算 / LevelFailure 关卡中止 → [05d](05d-defend-losecontrol-settle.md)

## 验收

- [x] 近战士兵能击杀怪
- [x] 清场条件可检测（刷怪行全触发 + 怪全灭）
- [x] 远程士兵本片不施放普攻（留待 05c2）

## 依赖

- [05b](05b-defend-spawn-path.md)

## 编码前

难度 3：须方案比选后再动手。**负责人 2026-07-26 选定方案 A**（Session 规则中枢 + WarriorAgentView）；工作量 **再拆**：本片近战+清场检测，远程见 05c2。

## 实现（SPEC v0.39.0，方案 A）

- 规则：`CombatConvertCoeffs` / `WarriorCombatMath`；`DefendSessionService` 登记兵/怪 HP、近战 HitConfirm、怪→兵 AttackPower、CombatDead、`ClearVictoryConditionDetected`
- 表现：`WarriorAgentView`（EngageZone 最近怪 + NavMesh + 前摇）；`MonsterAgentView` 可扣兵 HP / 死亡停用
- 接线：`DefendStageController` 开战注册士兵、刷怪注册怪物、HUD 清场可检测
- 远程：`AttackMode=Ranged` 仅日志，不攻击
