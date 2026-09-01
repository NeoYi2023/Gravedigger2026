---
title: Defend 尸体投射垂直切片
status: done
difficulty: 2
demo_scope: in-scope
approach: A
spec_refs:
  - SPEC_03 §3.12 DeathCorpseProjectile
  - SPEC_04 §15.5
  - SPEC_03 §3.8 D-083
completion_notes: |
  MonsterAgentView：抛物线 Tick + CorpseProjectileSmashSweep；alreadyHit 每飞行重置；SetCorpseSmashBridge。
  DefendStageController：ApplyMonsterDeathPresentation；CorpseSmash 致死 knockbackDistance=0。
  DefendSessionService.MonsterKilled 增 deathTag；砸击路径传 "CorpseSmash"。
---

## 目标

Defend 对等接线：`MonsterAgentView` 抛物线 + 砸击扫掠；`DefendStageController` 致命击表现；**无** PushMap 假死分支。

## 范围

1. **`MonsterAgentView`：** 镜像 CP-03 飞行/扫掠/落地/`alreadyHit`（可抽共享 `CorpseProjectileFlightState` 静态/helper 减少重复）
2. **`DefendStageController`：** `MonsterKilled` → `ApplyMonsterDeathPresentation`；CorpseSmash 致死 `knockbackDistance=0`
3. **`DefendSessionService.TryApplyCorpseSmashDamage`** 已在 CP-02 落地；本片只接线 View→Session
4. **FX：** 若 Defend 已有 `MonsterDamageSettled` 或等价事件则接飘字；否则 **最小** Console 日志 + HP 变化（SPEC 未强制 Defend 飘字）

## 不做

- PushMap 假死/倒放（D-074 Defend 不接线）
- 改 Defend 胜负入账

## 手验

- [ ] Defend 开战：高伤击杀怪抛物线 + 砸邻怪；低伤仅飞不砸
- [ ] 砸死不连锁
- [ ] 清场/护盾逻辑 **无**回归

## 依赖

- [02-rules-corpse-smash.md](02-rules-corpse-smash.md)
- 建议排在 [03-pushmap-vertical.md](03-pushmap-vertical.md) 之后（共享 helper 已稳定）

## 编码前

- 难度 2：可直接编码
