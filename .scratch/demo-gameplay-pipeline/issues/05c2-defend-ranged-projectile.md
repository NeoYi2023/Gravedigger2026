---
title: Defend — 士兵远程弹道命中方案 D
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12 WarriorCombat / 命中方案 D（远程）
  - SPEC_04 §9.9b ClassConfig（RangedProjectileSpeed / RangedTimeoutSeconds）
---

## 目标

`AttackMode=Ranged` 士兵按方案 D：进距 → 生成弹道 → 碰撞命中或超时未命中；规则层确认 `NormalAttackPower` 伤害。

## 范围

- `ProjectileView`（或等价）+ Session `HitConfirm`
- 弹速 / 超时取自 ClassConfig
- 与近战共用 EngageZone 选敌与攻速周期

## 不做

- 技能施放；法师/射手通道差异以外的特效 polish
- 清场胜利结算（仍见 05d）

## 验收

- [x] 远程士兵可击杀 EngageZone 内怪；超时未命中不扣血

## 依赖

- [05c](05c-defend-warrior-combat.md)

## 编码前

难度 2：须方案比选后再动手。**负责人 2026-07-26 选定方案 A**（View 驱动弹道 + Session `TryConfirmRangedHit`；软碰撞距离命中）。

## 实现（SPEC v0.40.0，方案 A）

- 规则：`DefendCombatWarriorState` 登记 `RangedProjectileSpeed` / `RangedTimeoutSeconds`；`TryConfirmRangedHit`
- 表现：`WarriorAgentView` 远程分支共用 EngageZone/ASPD；`ProjectileView` 运动学飞向锁定怪（距离≤hitRadius 命中；超时 Miss 不扣血）
- Prefab：`Assets/Prefabs/Defend/Projectile.prefab`（Catalog 绑定）；AssetBuilder 可重生
- 接线：`DefendStageController` Bind 传入 Projectile Prefab + worldRoot
