---
title: Defend — 士兵 EngageZone 选敌与普攻命中方案 D
status: todo
difficulty: 3
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12 WarriorCombat / 命中方案 D / 战斗派生公式
  - SPEC_04 §9.7 EngageZone
  - SPEC_04 §9.9b ClassConfig
  - SPEC_04 §9.19 MonsterConfig
---

## 目标

非叛变士兵仅在 EngageZone 内选最近怪；近战前摇 / 远程弹道按 ClassConfig；Demo 仅普攻。

## 范围

- NormalAttackPower / AttackSpeed 派生
- 怪物对士兵：AttackPower 直接扣 HP（无护甲）
- CombatDead 最小行为（停手即可）

## 不做

- 技能施放、复活技能

## 验收

- [ ] 士兵能击杀怪；清场条件可检测（刷怪行全触发 + 怪全灭）

## 依赖

- [05b](05b-defend-spawn-path.md)

## 编码前

难度 3：须方案比选后再动手。
