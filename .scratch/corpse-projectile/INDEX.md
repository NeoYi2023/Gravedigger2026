# 怪物尸体投射 DeathCorpseProjectile（D-083）

**状态：** SPEC **v0.83.47**；**Demo 实现完成**（§3.12 / §3.14 / SPEC_04 §15.5 / §9.20b）；Correctness 菜单 `Run Corpse Projectile Correctness Checks (D-083)`。

**背景：** 抛物线击飞 + 尸体砸人 **合一**；`distance≥DeathDie2KnockbackThreshold` 才砸；`OutgoingDamage×DeathCorpseSmashDamageMul`；途中+落地各一次、同怪只结算一次；仅砸存活怪；砸死不连锁；`MonsterCombatDead` 亦飞砸后 Delay→倒放。

**工作量：** 整体难度 **3**（跨规则层 + 双战场 View + 假死时序）；**须拆步**；每 Agent 会话 **最多一片**。

**选定方案（实现）：** **方案 A** — Session `TryApplyCorpseSmashDamage` 结算 HP；View 驱动尸体 `Transform` 抛物线并每帧/落地调 Session 扫掠；**不**新建独立 `ProjectileView` 预制体；`alreadyHit` 由飞行中的尸体 View 持有。

## 切片一览

| ID | 文件 | 依赖 | 难度 | 状态 |
|----|------|------|------|------|
| CP-00 | [00-spec-close.md](issues/00-spec-close.md) | — | 1 | **done** |
| CP-01 | [01-constants-parabolic-core.md](issues/01-constants-parabolic-core.md) | CP-00 | 2 | **done** |
| CP-02 | [02-rules-corpse-smash.md](issues/02-rules-corpse-smash.md) | CP-01 | 3 | **done** |
| CP-03 | [03-pushmap-vertical.md](issues/03-pushmap-vertical.md) | CP-02 | 3 | **done** |
| CP-04 | [04-defend-vertical.md](issues/04-defend-vertical.md) | CP-02 | 2 | **done** |
| CP-05 | [05-correctness-handcheck.md](issues/05-correctness-handcheck.md) | CP-03, CP-04 | 2 | **done** |

## 推荐执行序

```
CP-00 → CP-01 → CP-02 → CP-03 → CP-04 → CP-05
```

- **CP-03 与 CP-04** 可在 CP-02 完成后 **并行**（不同 Stage/View），但共享 `MonsterDeathPresentation` / Session API，建议 **先 PushMap（含假死）再 Defend** 以降低回归风险。
- **预估：** 4～5 个编码会话（不含 CP-00）。

## 并行与风险

| 风险 | 缓解 |
|------|------|
| 假死怪飞砸中击杀其它怪触发连锁 | Session `CorpseSmash` 致死走 `knockbackDistance=0` 表现 |
| 飞砸未完成就 `DeathPresentationComplete` → 提前复活 | `TryNotifyDeathPresentationComplete` 仍等 `_deathKnockActive=false` |
| 砸击与 Comfort/D-073 叠乘 | 规则层独立通道，不经过 `SettleMonsterDamage` |
| Defend 无 `MonsterDamageSettled` 飘字 | CP-04 最小：日志 + HP；可选接 Defend 现有事件 |

## 权威

- [SPEC_03 §3.12 DeathCorpseProjectile](../../SPEC_03_GameRules.md)
- [SPEC_03 §3.8 D-083](../../SPEC_03_GameRules.md)
- [SPEC_04 §15.5](../../SPEC_04_Technical.md)、[§9.20b](../../SPEC_04_Technical.md)
- [CONTEXT DeathCorpseProjectile](../../CONTEXT.md)

分步粘贴指令：[new-agent-prompts.md](new-agent-prompts.md)
