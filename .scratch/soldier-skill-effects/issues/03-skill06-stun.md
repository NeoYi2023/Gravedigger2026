---
title: Skill_06 震晕（普攻命中 AOE 击晕）
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12 SkillCast
  - SPEC_03 §3.8 D-073
  - SPEC_04 §9.21 Skill_06 样例
  - SPEC_04 §9.21b OnAaHitChanceAoeStun
selected_approach: B+ — CombatStatusService.Stun + OnWarriorAaHitConfirm Handler
---

## 目标

`Skill_06` 震晕 Lv1～5：士兵**普攻命中**敌人后，**10%** 概率对**目标 + 半径 1.5** 内所有存活敌人施加**击晕 1～5 秒**（无法攻击与移动）。

## 范围

- `CombatStatusService.Stun`：怪物侧；Stun 期间跳过 AI 移动/普攻 Tick（PushMap `PushMapMonsterAgentView` 或 Session 查询 gate）
- Handler `OnAaHitChanceAoeStun` @ `OnWarriorAaHitConfirm`（近战 HitConfirm + 远程弹道命中均算普攻命中）
- Params：`Chance=0.1`，`Radius=1.5`，`StunSeconds=1..5`（等级差在各行 EffectParams）
- XZ 圆心 = 被命中怪位置；`ContainsXZ` 或距离 ≤ Radius
- 表 + `EffectImplemented=1`

## 不做

- 击晕士兵 / 主角
- 100% 触发或等级改概率（CSV 全级 10%，除非 SPEC 修订）
- Defend

## 验收

- [x] 炸弹师（`Class_BombMaster`）普攻命中后日志可见 AOE Stun；Stun 中怪不移动不攻击
- [x] 时长 Lv1～Lv5 可区分
- [x] 经 Pipeline + Status；勾 issue；INDEX SE-03→done

## 依赖

- [SE-01](01-skill04-first-strike.md)；[SE-02](02-skill05-tenacity.md)（CombatStatusService 已有 Invincible 模式可参考）

## 编码前

- 难度 **2**；选定方案 **B+**
