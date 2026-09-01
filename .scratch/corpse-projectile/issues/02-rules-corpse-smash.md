---
title: 规则层尸体砸击结算
status: done
difficulty: 3
demo_scope: in-scope
approach: A
spec_refs:
  - SPEC_03 §3.12 DeathCorpseProjectile
  - SPEC_04 §15.5
  - SPEC_03 §3.8 D-083
completion_notes: |
  连锁约定：砸击致死 Stage/View 用 NotifyKilled(knockbackDistance=0) 阻止新投射（未用显式 bool）。
  实现：CorpseSmashCombatMath + PushMap/Defend TryApplyCorpseSmashDamage；CorpseSmashCombatCorrectnessChecks。
---

## 目标

Session 提供 **独立砸击通道**：`CorpseSmashDamage = killerOutgoingDamage × DeathCorpseSmashDamageMul`；砸击致死 **不连锁**尸体投射；PushMap 假死 / 真死分支正确。

## 范围

1. **共享辅助（建议 `Core/Combat/CorpseSmashCombatMath.cs`）：**
   - `ComputeSmashDamage(killerOutgoingDamage)`
   - `IsWithinSmashHitRadius(corpseXZ, targetXZ, targetBodyRadius)`
2. **`PushMapSessionService`：**
   - `bool TryApplyCorpseSmashDamage(string corpseRuntimeId, string killerWarriorId, float killerOutgoingDamage, string targetRuntimeId)`
   - 目标须 `IsMonsterTargetable`、非 `corpseRuntimeId`、存活
   - 扣 HP → `MonsterDamageSettled`（供飘字/闪）→ `TryFinalizeMonsterDeath(..., tag: "CorpseSmash")`
   - **不**走 `SettleMonsterDamage` / Comfort / D-073
   - `TryFinalizeMonsterDeath`：`CorpseSmash` 致死与普攻致死 **同一**击杀/假死分支；`MonsterKilled` / `MonsterEnteredCombatDead` 的 `outgoingDamage` 传 **砸击伤害值**（日志用）
3. **`DefendSessionService` 对等 API**（Melee/Ranged 致死逻辑对齐：砸击致死 `IsAlive=false` + `MonsterKilled`）
4. **连锁（5B）：** Stage/View 约定：仅当致命击 **非** `CorpseSmash` 来源且 `knockbackDistance>0` 时启动抛物线；砸击致死 `NotifyKilled(knockbackDistance=0)` 或显式 `skipCorpseProjectile` 标志（实现二选一，写入 issue 完成备注）

## 不做

- View 抛物线 Tick（CP-03/04）
- 士兵友伤
- 新技能 EffectKind

## 验收

- [x] PushMap：mock 两次 `TryApplyCorpseSmashDamage` 同目标只扣一次（由 View `alreadyHit` + Session 目标校验；Session 单测或 Correctness 覆盖 **伤害公式**）
- [x] 砸击伤害 = `killerOutgoing × Mul`，与 NAP/Comfort 无关
- [x] 砸击致死触发 `MonsterKilled`；**不**要求 View 再飞（由 CP-03 接线验证）
- [x] 砸击致死若目标有复活技能 → `MonsterEnteredCombatDead`（PushMap）
- [x] Defend：`TryApplyCorpseSmashDamage` 扣血并 `MonsterKilled`

## 依赖

- [01-constants-parabolic-core.md](01-constants-parabolic-core.md)

## 编码前

- 难度 3：AskQuestion 确认「砸击致死表现」用 `knockbackDistance=0` **或** 显式 bool（推荐 **knockbackDistance=0**）
