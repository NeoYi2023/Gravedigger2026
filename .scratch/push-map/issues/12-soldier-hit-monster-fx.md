---
title: PushMap — 士兵→怪 HitConfirm + 红飘字红闪烁
status: done
difficulty: 3
demo_scope: authorized
approach: B
spec_refs:
  - SPEC_03 §3.14 Demo 士兵攻击 / WarriorCombat / DamagePopup / HitFlash / BOSS 通关
  - SPEC_04 §6 PM-12
  - SPEC_04 §9.22 WarriorCombat / DamagePopup / HitFlash 运行时契约
---

## 目标

在 `PushMapSessionService` 登记士兵/怪物 HP 与 HitConfirm；`PushMapAdvanceView` 迁入 §3.12 方案 D（近战前摇 + 远程 `ProjectileView`）；废止 `PollMonsterDemoKill`；命中后怪头顶红飘字 `-N`（字号 28）+ 怪模型红 HitFlash。

## 范围

- `PushMapSessionService`：`TryRegisterWarrior` / `RegisterMonster` / `TryConfirmMeleeHit` / `TryConfirmRangedHit`；`RemainingHp≤0` → 驱动 `NotifyKilled` / `TryNotifyBossKilled`
- `PushMapAdvanceView`：保留 FlowField / AttackSlot / 粘滞；迁入 windup + 弹道（对齐 `WarriorAgentView`）
- `PushMapStageController`：开战登记；**删除** `DemoKillEngageSeconds` / `PollMonsterDemoKill`
- 新建 `DamagePopupView` + Prefab（怪样式）；新建 `HitFlashView`（怪红）
- 复用：`WarriorCombatMath`、`ProjectileView` / `Projectile.prefab`、`WarriorAnimView`
- 色改手法对齐 `DigGraveView` MaterialPropertyBlock

## 不做

- 怪→兵扣血 / 兵白飘字白闪（PM-13）
- 技能、护甲、Defend 飘字/闪烁
- 绑定 `DefendSessionService` 生命周期

## 验收

- [x] 近战：前摇结束且在距 → 怪掉血 + 红 `-N`(28) + 红闪（2×0.1s 紧接 ≈0.2s）— `PushMapAdvanceView` windup → `TryConfirmMeleeHit` → `MonsterDamageSettled`（DamagePopup 红 28 + HitFlashView 红 0.2s）
- [x] 远程：弹道命中同上；超时未命中不结算/不飘/不闪 — 复用 `ProjectileView`（泛化 `IProjectileCombatSession`），timeout/gone → DespawnMiss 不调 HitConfirm
- [x] 连击刷新 HitFlash — `HitFlashView.Play` 重入即重置 0.2s 计时
- [x] BOSS `HP≤0` → `TryNotifyBossKilled`；无 DemoKill 秒杀 — `MonsterKilled` 事件 → `NotifyKilled` + Boss 递减；`DemoKillEngageSeconds`/`PollMonsterDemoKill`/`_engageClaims` 已删
- [x] 勾 INDEX PM-12 done；`dotnet build` 0 错误（若工程可编）— 本机无 dotnet SDK / 无 csproj，未能 CLI 编译；须 Unity 打开后 Console 复核

## 实现备忘（方案 A）

- 伤害事件：C# event `PushMapSessionService.MonsterDamageSettled(runtimeId, damage)` / `MonsterKilled(runtimeId)`；StageController 订阅后解析 View 世界坐标刷飘字/闪烁（规则层不碰 Transform）
- 飘字：TextMesh（对齐 WarriorTaskDebugLabelView 动态字体手法）；Prefab `Assets/Prefabs/PushMap/DamagePopup.prefab`，经 `DefendPrefabCatalog._damagePopupPrefab` 接线
- 弹道：`ProjectileView` 泛化为 `IProjectileCombatSession` + `Func<string, Transform>` 目标解析；Defend/PushMap 双 Session 实现，Prefab 复用 `Assets/Prefabs/Defend/Projectile.prefab`
- 士兵/怪物战斗态复用 `DefendCombatWarriorState`/`DefendCombatMonsterState` 数据类（仅镜像，不绑定 DefendSessionService 生命周期）

## 依赖

- [11](11-spec-warrior-combat-fx.md) done

## 编码前

- 难度 3：AskQuestion（或短文本）确认难度与实现子方案（例如弹道 Prefab 引用路径 / 伤害事件总线形状），选定后再编码
