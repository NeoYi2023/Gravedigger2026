# PushMap — 多 Agent 垂直切片

**状态：** 规则已录入 SPEC v0.52.0（§3.14 / SPEC_04 §9.22–§9.23）；PM-01 样例标记已落地（v0.54.0）；PM-02 配置表 Bake/CSV 加载已落地（v0.55.0）；PM-03 Stage 接线已落地（v0.56.0）；PM-04 目标点链与判定圈占领已落地（v0.57.0）；PM-05 刷怪点与陷阱已落地（v0.58.0）；PM-06 AggroMode 四态已落地（v0.59.0）；PM-07 BOSS 通关与奖励钩子已落地（v0.60.0）；PM-08 空气墙 NavMesh 已落地（v0.61.0）。**Demo 已授权**（2026-08-06）：按 issue 分 Agent 编码；每会话最多推进一个无阻塞切片。

**工作量：** 整体难度 3，一律拆步；每 issue 独立 Agent 会话。

## 切片一览

| ID | 文件 | 依赖 | 难度 | 状态 |
|----|------|------|------|------|
| PM-00 | [00-spec-close.md](issues/00-spec-close.md) | — | 2 | done（本会话规则录入） |
| PM-01 | [01-map-prefab-markers.md](issues/01-map-prefab-markers.md) | PM-00 | 2 | done（方案 A：`PushMap_Demo_01` + 标记组件） |
| PM-02 | [02-config-tables.md](issues/02-config-tables.md) | PM-00 | 2 | done（方案 A：PushMap 表 + AggroMode 加载） |
| PM-03 | [03-stage-module-wire.md](issues/03-stage-module-wire.md) | PM-02 | 3 | done（方案 A：`PushMapStageModule`+`PushMapSessionService`+`PushMapStageController`） |
| PM-04 | [04-objective-capture.md](issues/04-objective-capture.md) | PM-01, PM-03 | 3 | done（方案 A：`PushMapSessionService` 目标链/占领 + Probe/AdvanceView + StageController 接线） |
| PM-05 | [05-spawn-trap.md](issues/05-spawn-trap.md) | PM-01, PM-02, PM-04 | 3 | done（方案 A：`PushMapSessionService` 刷怪装载/开战触发/陷阱触发/占领停刷 + `PushMapStageController`/`PushMapMonsterAgentView`） |
| PM-06 | [06-aggro-mode.md](issues/06-aggro-mode.md) | PM-05 | 3 | done（方案 A：`PushMapMonsterAgentView` 四态 + `NotifyProvoked` 挑衅接线） |
| PM-07 | [07-boss-clear-rewards.md](issues/07-boss-clear-rewards.md) | PM-05 | 2 | done（方案 A：`TryNotifyBossKilled`/`VictorySettled`/`DungeonUnlockService`） |
| PM-08 | [08-air-wall-navmesh.md](issues/08-air-wall-navmesh.md) | PM-01, PM-03 | 2 | done（方案 A：Bake 注入 Not Walkable Box） |

## 并行建议

- PM-01 ∥ PM-02
- PM-06 可与 PM-07 部分并行
- PM-08 可与 PM-04～05 并行

## 权威

- [SPEC_03 §3.14](../../SPEC_03_GameRules.md)
- [SPEC_04 §9.22–§9.23](../../SPEC_04_Technical.md)、[§9.19 AggroMode](../../SPEC_04_Technical.md)
