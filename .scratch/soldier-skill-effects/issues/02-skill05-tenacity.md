---
title: Skill_05 坚挺（致死拦截 + 无敌）
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12 SkillCast
  - SPEC_03 §3.8 D-073
  - SPEC_04 §9.21 Skill_05 样例
  - SPEC_04 §9.21b CheatDeathInvincible
selected_approach: B+ — CombatStatusService.Invincible + Pipeline OnWarriorWouldDie Handler
---

## 目标

`Skill_05` 坚挺 Lv1～5：当次攻击将使 Self HP≤0 时，拦截为 **HP=1** + **无敌 1～5 秒**；`BaseCooldownSeconds=60`（Mode2 提交后进 CD）。

## 范围

- `CombatStatusService`：实现 **Invincible**（到期自动清；无敌期间 `OnIncomingDamageSettle` 伤害→0 或 Skip）
- Handler `CheatDeathInvincible` @ `OnWarriorWouldDie`（怪物普攻/技能皆可，对齐 `ExtraActivationCondition=敌人的本次攻击导致Self死亡`）
- CD：`TryCommitSkillInternalCooldown` 或 Pipeline 等价；成功触发后写 60s CD
- CombatSkillIcon：触发时 `SkillIconPopup`；无敌持续可选 `SkillPersistChanged`
- 表：`SkillEffect_05_1`～`_5` → `InvincibleSeconds=1..5`；`EffectImplemented=1`

## 不做

- 复活已 `CombatDead` 士兵
- Defend 接线
- 其它 CC

## 验收

- [x] 近卫（`Class_Guardian`）致死一击变 1 HP + 无敌；无敌期间不再扣血
- [x] Lv1 1s vs Lv5 5s 可区分；CD 60s 内不重复触发
- [x] Handler 经 Pipeline；无 `Skill_05` Session 分支
- [x] 全等级绿指示器；勾 issue；INDEX SE-02→done

## 依赖

- [SE-01](01-skill04-first-strike.md)（Pipeline + CombatStatusService 壳）

## 编码前

- 难度 **2**
