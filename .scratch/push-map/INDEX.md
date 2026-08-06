# PushMap — 多 Agent 垂直切片

**状态：** 规则已录入 SPEC v0.52.0（§3.14 / SPEC_04 §9.22–§9.23）。**Demo 已授权**（2026-08-06）：按 issue 分 Agent 编码；每会话最多推进一个无阻塞切片。

**工作量：** 整体难度 3，一律拆步；每 issue 独立 Agent 会话。

## 切片一览

| ID | 文件 | 依赖 | 难度 | 状态 |
|----|------|------|------|------|
| PM-00 | [00-spec-close.md](issues/00-spec-close.md) | — | 2 | done（本会话规则录入） |
| PM-01 | [01-map-prefab-markers.md](issues/01-map-prefab-markers.md) | PM-00 | 2 | todo |
| PM-02 | [02-config-tables.md](issues/02-config-tables.md) | PM-00 | 2 | todo |
| PM-03 | [03-stage-module-wire.md](issues/03-stage-module-wire.md) | PM-02 | 3 | todo |
| PM-04 | [04-objective-capture.md](issues/04-objective-capture.md) | PM-01, PM-03 | 3 | todo |
| PM-05 | [05-spawn-trap.md](issues/05-spawn-trap.md) | PM-01, PM-02, PM-04 | 3 | todo |
| PM-06 | [06-aggro-mode.md](issues/06-aggro-mode.md) | PM-05 | 3 | todo |
| PM-07 | [07-boss-clear-rewards.md](issues/07-boss-clear-rewards.md) | PM-05 | 2 | todo |
| PM-08 | [08-air-wall-navmesh.md](issues/08-air-wall-navmesh.md) | PM-01, PM-03 | 2 | todo |

## 并行建议

- PM-01 ∥ PM-02
- PM-06 可与 PM-07 部分并行
- PM-08 可与 PM-04～05 并行

## 权威

- [SPEC_03 §3.14](../../SPEC_03_GameRules.md)
- [SPEC_04 §9.22–§9.23](../../SPEC_04_Technical.md)、[§9.19 AggroMode](../../SPEC_04_Technical.md)
