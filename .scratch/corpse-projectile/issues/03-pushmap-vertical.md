---
title: PushMap 尸体投射垂直切片
status: done
difficulty: 3
demo_scope: in-scope
approach: A
spec_refs:
  - SPEC_03 §3.12 DeathCorpseProjectile
  - SPEC_03 §3.14 Demo 怪物死亡复活边界
  - SPEC_04 §15.5
  - SPEC_03 §3.8 D-083
completion_notes: |
  PushMapMonsterAgentView：抛物线 Tick + 飞行/落地 CorpseProjectileSmashSweep；alreadyHit 每飞行重置。
  Stage：SetCorpseSmashBridge → TryApplyCorpseSmashDamage；EnumerateLivingMonstersForCorpseSmash 用 IsMonsterTargetable。
  MonsterKilled/MonsterEnteredCombatDead 增 deathTag；CorpseSmash 致死 knockbackDistance=0 不连锁。
---

## 目标

PushMap 完整接线：抛物线尸体飞行 + 达阈值飞行扫掠/落地砸怪 + 假死 Delay→倒放时序不变 + 红飘字/红闪（复用 `MonsterDamageSettled`）。

## 范围

1. **`PushMapMonsterAgentView.NotifyKilled`：**
   - 存 `killerWarriorId`、`killerOutgoingDamage`、`knockbackDistance`
   - `TickDeathKnockback` → `TrySampleParabolicKnockback`；飞行中若 `ShouldEnableCorpseSmash` → `TickCorpseSmashSweep`
   - 落地瞬间（`t` 从 &lt;1 到 ≥1）→ `TryCorpseSmashLanding`
   - `HashSet<string> _corpseSmashHit` 每飞行重置
   - 枚举其它存活怪：Stage 提供 `EnumerateLivingMonsterRuntimeIds()` 或 Session 列表 + `FindMonsterView` 取 XZ/`BodyRadius`
2. **`PushMapStageController.ApplyMonsterDeathPresentation`：**
   - 致命击（非 CorpseSmash）照旧算 `distance` 并 `NotifyKilled`
   - 监听 `MonsterKilled` / `MonsterEnteredCombatDead`：若死亡 tag 为 CorpseSmash → `NotifyKilled(knockbackDistance: 0)` **或** 仅 latch 分支
3. **假死（6A）：** `MonsterCombatDead` 仍 `NotifyKilled`+抛物线；`TryNotifyDeathPresentationComplete` 仍在击飞结束后；砸击不提前触发复活
4. **FX：** 砸击 `MonsterDamageSettled` → 现有红 `DamagePopup` + 红 `HitFlash`（PM-12 链）
5. **NavMesh / MassMove：** 飞行尸体已 Unregister；砸中的活怪不受影响

## 不做

- Defend（CP-04）
- 落地尘土 VFX
- BOSS/Loot 规则变更

## 手验场景（PushMap_Demo_01 或 GM）

- [ ] 高伤击杀：`distance≥1` 怪抛物线飞起，途中/落地砸到邻近怪（红字），同怪只伤一次
- [ ] 低伤击杀：`distance&lt;1` 仅抛物线，邻近怪 **无伤**
- [ ] 砸死第二只怪：第二只 **不**再飞出砸人（原位 Die latch）
- [ ] 带复活技能怪：飞砸后仍 Delay→倒放复活；无敌/变暗契约不变
- [ ] BOSS 击杀/假死不计击杀边界 **不变**

## 依赖

- [02-rules-corpse-smash.md](02-rules-corpse-smash.md)

## 编码前

- 难度 3：确认手验地图与测试怪布局（密集刷怪点）
