---
title: PushMap — 刷怪点与陷阱触发
status: done
difficulty: 3
demo_scope: authorized
approach: A
spec_refs:
  - SPEC_03 §3.14 刷怪
  - SPEC_04 §9.23 PushMapSpawnConfig
---

## 目标

无陷阱：开战且关联目标未占领 → 刷怪；有陷阱：忠诚士兵首次进入 TrapZone → 刷怪；占领后停新刷，已刷怪保留。

## 范围

- 一点多 MonsterId 行；SpawnOrder
- **不**使用 WaveSpawn 倒计时

## 不做

- AggroMode 四态完整（PM-06 可先用 Defend 默认追击）
- BOSS 通关结算（PM-07）

## 验收

- [x] 无陷阱开战刷可验（开战即刷 `SP_01` 关联目标1未占 + `Boss_01` 全局 BOSS；日志 `[PushMapSession] Spawn (StartBattle)`）
- [x] 陷阱触发一次可验（忠诚兵首次进 `TrapZone TZ_01` → `SP_02` 刷出；`[PushMapSession] Trap 'TZ_01' fired`；重复进入不重复刷）
- [x] 占领后不再新刷；场上怪仍在（`ObjectiveCaptured` → 关联点停刷集；已刷怪保留）

## 依赖

- [01](01-map-prefab-markers.md)
- [02](02-config-tables.md)
- [04](04-objective-capture.md)

## 编码前

- 难度 3 方案比选；须 Demo 授权

## 本会话交付（方案 A）

- SPEC：SPEC_04 §6 增 PM-05 段、§9.23 增「刷怪运行时契约」（中英）；SPEC_03 §3.14 增「Demo 刷怪 AI 边界」（中英）；SPEC_00 v0.58.0
- 规则：`Assets/Scripts/Core/PushMap/` 新增 `PushMapSpawnRequest.cs`（`PushMapSpawnTrigger` + 请求负载）；`PushMapSessionService` 增刷怪行装载/排序、开战触发（无陷阱+未占）、`TryNotifyTrapEnter`（首次/每点一次）、`ObjectiveCaptured` 停刷集（IsBoss 不受停刷）；事件 `PushMapSpawnRequested`
- 表现：`Assets/Scripts/Gameplay/PushMap/` 新增 `PushMapMonsterAgentView.cs`（NavMeshAgent；Defend 默认追击语义；对主角经 `ApplyShieldHit` 扣盾；对兵日志）；`PushMapStageController` 收集 `SpawnPoint`/`TrapZone`/`BossPoint`、订阅刷怪事件 Instantiate 入 `_monsters`（Boss 用 BossPoint 位置；缺标记回退并警告）、Update 探测忠诚兵首次进圈、`PushMapMonsterPresenceProbe` 经 shim（`MonsterAgentView.BindProbeOnly` + `SyncAliveFrom`）读存活怪
- 兼容：`MonsterAgentView`（Defend）增 `BindProbeOnly` / `SyncAliveFrom` / `_probeOnly` 短路，不影响 Defend 原行为
- 配置：`PushMap_PushMapSpawnConfig.csv` 样例 `TrapZoneId` 由 `Trap_01` 对齐为 `TZ_01`（与地图标记一致）
- 边界：不使用 `WaveSpawnConfig` 倒计时；AggroMode 四态 / BOSS 通关结算 / 经验入账不做（PM-06/07）；士兵 HP 暂不追踪（对兵伤害仅日志）
- 工程：`Assembly-CSharp.csproj` 追加 2 个新脚本 Compile 项；`dotnet build` 0 错误
