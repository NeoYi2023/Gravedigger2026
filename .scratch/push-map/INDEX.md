# PushMap — INDEX

**状态：** 规则已录入至 SPEC **v0.75.0**（§3.14 / SPEC_04 §9.22～§9.23）；PM-01～PM-10 已落地；**PM-11 SPEC（WarriorCombat + DamagePopup + HitFlash）已关闭**；编码切片 **PM-12 → PM-13**（方案 B）。**Demo 已授权**：按 issue 分 Agent 编码；每会话最多推进一个无阻塞切片。

**工作量：** PM-11～13 整体难度 3，一律拆步；每 issue 独立 Agent 会话。

## 切片一览

| ID | 文件 | 依赖 | 难度 | 状态 |
|----|------|------|------|------|
| PM-00 | [00-spec-close.md](issues/00-spec-close.md) | — | 2 | done |
| PM-01 | [01-map-prefab-markers.md](issues/01-map-prefab-markers.md) | PM-00 | 2 | done |
| PM-02 | [02-config-tables.md](issues/02-config-tables.md) | PM-00 | 2 | done |
| PM-03 | [03-stage-module-wire.md](issues/03-stage-module-wire.md) | PM-02 | 3 | done |
| PM-04 | [04-objective-capture.md](issues/04-objective-capture.md) | PM-01, PM-03 | 3 | done |
| PM-05 | [05-spawn-trap.md](issues/05-spawn-trap.md) | PM-01, PM-02, PM-04 | 3 | done |
| PM-06 | [06-aggro-mode.md](issues/06-aggro-mode.md) | PM-05 | 3 | done |
| PM-07 | [07-boss-clear-rewards.md](issues/07-boss-clear-rewards.md) | PM-05 | 2 | done |
| PM-08 | [08-air-wall-navmesh.md](issues/08-air-wall-navmesh.md) | PM-01, PM-03 | 2 | done |
| PM-09 | [09-camera-follow.md](issues/09-camera-follow.md) | PM-03, PM-04 | 2 | done |
| PM-10 | [10-monster-body-spread.md](issues/10-monster-body-spread.md) | PM-05 | 2 | done |
| PM-11 | [11-spec-warrior-combat-fx.md](issues/11-spec-warrior-combat-fx.md) | PM-01～10 | 3 | **done**（SPEC + 切片；本会话不编程） |
| PM-12 | [12-soldier-hit-monster-fx.md](issues/12-soldier-hit-monster-fx.md) | PM-11 | 3 | todo（下一编码片） |
| PM-13 | [13-monster-hit-soldier-fx.md](issues/13-monster-hit-soldier-fx.md) | PM-12 | 3 | todo |

## 并行建议

- PM-12 与 PM-13 **不可**并行（PM-13 依赖 PM-12 的 Session/飘字/闪烁骨架）
- New Agent 话述见 [new-agent-prompts.md](new-agent-prompts.md)

## 权威

- [SPEC_03 §3.14](../../SPEC_03_GameRules.md)
- [SPEC_04 §6 / §9.22](../../SPEC_04_Technical.md)
- 计划：PushMap 命中结算、伤害飘字与受伤闪烁（方案 B）
