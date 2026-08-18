---
title: 战斗施放管线 + Skill_03 连发（主动）
status: done
difficulty: 3
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12 WarriorCombat / SkillCast
  - SPEC_03 §3.8 D-069
  - SPEC_04 §9.21 SkillConfig（Skill_03 样例）
  - SPEC_04 §9.21b SkillEffect_03_1～_5
  - SPEC_04 §9.7 PushMapSessionService / DefendSessionService
selected_approach: C — 占用普攻通道 3×方案 D；PushMap；进距且 CD 好即放
---

## 目标

非叛变士兵在 Combat 中按 `SoldierSkills` + `SkillConfig` **自动施放** `Skill_03`（连发）：对当前攻击目标连续走方案 D **3 次**；CD 随等级 50/40/30/20/10s（`Mode2`：提交后进 CD）。

## 范围

- Core：每名士兵运行时 CD 状态；查 `SkillConfig(SkillId, SkillLevel)`；`SkillCooldown` 公式驱动
- 解析：`CastTarget=EnemySingle`；`ExtraActivationCondition` 空
- 与普攻：不破坏 EngageZone / AttackSlot / FormationHome；CD 好且进距即放（不打断进行中前摇）
- 结算：3 次 `NormalAttackPower`（复用 HitConfirm HP 通道）
- 失控：`ΣSkillBonus≠0` 时每次成功提交后再 roll（日志可观察）
- Gameplay：View 占用攻击通道；Debug 标签可显示 CD / 连发
- 战场：**PushMap**

## 不做

- `Skill_01` 被动（→ SC-02）
- `Skill_04`～`Skill_12`、Mode1 战斗技能
- UI 技能栏 / 手动按键施放
- `SkillEffectConfig` 全表通用解析器（硬映射 `SkillEffect_03_*` hitCount=3）
- Defend 接线

## 验收

- [x] Mode2 法师（`Class_Mage` / `Skill_03`）在 PushMap 对单体敌人触发连发；日志 3 次伤害（方案 D 命中）
- [x] CD 受 `BaseCooldownSeconds` + `SkillCooldown` 公式约束；Lv1～5 可区分
- [x] 叛变士兵不误触发本技能逻辑
- [x] D-069 首条勾选；勾 issue；INDEX SC-01→done

## 依赖

- [SC-00](00-spec-close.md)（done）
- D-062 士兵技能数据垂直（done）
