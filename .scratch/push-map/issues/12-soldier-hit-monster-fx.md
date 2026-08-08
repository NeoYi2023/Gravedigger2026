---
title: PushMap — 士兵→怪 HitConfirm + 红飘字红闪烁
status: todo
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

- [ ] 近战：前摇结束且在距 → 怪掉血 + 红 `-N`(28) + 红闪（2×0.1s 紧接 ≈0.2s）
- [ ] 远程：弹道命中同上；超时未命中不结算/不飘/不闪
- [ ] 连击刷新 HitFlash
- [ ] BOSS `HP≤0` → `TryNotifyBossKilled`；无 DemoKill 秒杀
- [ ] 勾 INDEX PM-12 done；`dotnet build` 0 错误（若工程可编）

## 依赖

- [11](11-spec-warrior-combat-fx.md) done

## 编码前

- 难度 3：AskQuestion（或短文本）确认难度与实现子方案（例如弹道 Prefab 引用路径 / 伤害事件总线形状），选定后再编码
