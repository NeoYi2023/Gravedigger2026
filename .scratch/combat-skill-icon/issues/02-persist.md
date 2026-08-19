---
title: 战斗技能图标 — Skill_02 脚下持续 + CombatDead 清理
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.8 D-071
  - SPEC_03 §3.12 SkillCast 表现
  - SPEC_04 §9.22 CombatSkillIcon
selected_approach: A — SkillPersistChanged + 开战 EmitStartBattleSkillIcons；CombatDead ClearAll
---

## 目标

Skill_02 满血脚下 20×20 持续；生效瞬间同时头顶飘一次；受伤收起；死亡立即清该兵全部图标。

## 接线

- 开战且持有 Skill_02 且 `RemainingHp>=MaxHp` → Persist on + Popup（HUD 绑定后再 `EmitStartBattleSkillIcons`）
- HP 满→未满 → Persist off
- HP 未满→满（预留）→ Persist on + Popup
- 满血期间格挡伤害 0 **不**重播头顶
- `WarriorCombatDead` → `ClearAll`

## 验收

- [x] 满血脚下持续 + 生效头顶飘
- [x] 受伤收起；同 SkillId 脚下只 1 个
- [x] CombatDead / 销毁立即清图标
- [x] Defend 不接线
