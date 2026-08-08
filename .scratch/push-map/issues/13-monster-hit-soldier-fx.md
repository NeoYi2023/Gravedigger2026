---
title: PushMap — 怪→兵扣血 + 白飘字白闪烁
status: done
difficulty: 3
demo_scope: authorized
approach: B
combat_dead_fx: A
spec_refs:
  - SPEC_03 §3.14 Demo 士兵受击 / DamagePopup / HitFlash / AggroMode 挑衅
  - SPEC_04 §6 PM-13
  - SPEC_04 §9.22 WarriorCombat / DamagePopup / HitFlash 运行时契约
---

## 目标

怪物对忠诚士兵按 `AttackPower` 真实扣 HP；命中后兵头顶白飘字 `-N`（字号 24）+ 兵模型白 HitFlash；`HP≤0` → `CombatDead`；被动挑衅优先接真实 HitConfirm。

## 范围

- `PushMapSessionService.TryApplyMonsterDamageToWarrior`
- `PushMapMonsterAgentView`：对兵命中改调 Session（替换仅日志）
- 士兵白 `DamagePopup` / 白 `HitFlash`（复用 PM-12 组件，换样式）
- `CombatDead` 停手 + 最小死亡表现（对齐 §3.12 Demo）
- 挑衅：优先士兵对该怪 HitConfirm → `NotifyProvoked`；可保留进距兜底
- 主角仍 `ApplyShieldHit`（不要求主角飘字/闪烁）

## 不做

- 改 PM-12 士兵→怪命中链（除非 bugfix）
- 技能、护甲、Defend 飘字/闪烁、完整 PermanentDeath 物资去向 polish

## 验收

- [x] 怪打忠诚兵：扣 `AttackPower` + 白 `-N`(24) + 白闪（2×0.1s 紧接）— `TryApplyMonsterDamageToWarrior` → `WarriorDamageSettled` → StageController 白飘字/白闪
- [x] 连击刷新 HitFlash — 复用 `HitFlashView.Play` 重入重置
- [x] 士兵 `HP≤0` 停手 — `CombatDead` + `PushMapAdvanceView` `PlayDie` + `StopActing`（方案 A）
- [x] 被动怪可被真实命中激怒（或进距兜底仍可用）— `MonsterDamageSettled`→`NotifyProvoked`；`PollPassiveProvocation` 兜底
- [x] 勾 INDEX PM-13 done

## 实现备忘

- 选定：难度 3；CombatDead 最小表现 **方案 A**（`WarriorAnimView.PlayDie` + 停手；无 PermanentDeath 物资 polish；宝石仅 mark `IsPermanentDead`）
- 事件：`WarriorDamageSettled(warriorId, damage)` / `WarriorCombatDead(warriorId)`
- 怪 Bind 追加 `onHitWarrior` → Session；兵部署加 `HitFlashView`

## 依赖

- [12](12-soldier-hit-monster-fx.md)

## 编码前

- 难度 3：AskQuestion 确认难度与死亡表现最小方案，选定后再编码 — **已确认：难度 3 + CombatDead 方案 A**
