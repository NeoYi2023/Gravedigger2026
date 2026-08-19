---
title: Skill_02 满血脚下持续 + 开战/受伤/死亡清理
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.8 D-071
  - SPEC_03 §3.12 SkillCast 表现
  - SPEC_04 §9.22 CombatSkillIcon
selected_approach: A — SkillPersistChanged + SyncSkill02Persist
---

## 目标

持有 `Skill_02` 且满血的士兵：脚下 20×20 持续图标；生效瞬间同时头顶飘一次。受伤收起；`CombatDead` 立即清该兵全部图标。

## 范围

- `PushMapSessionService.SyncSkill02Persist` / `EmitStartBattleSkillIcons`
- 开战满血 → Persist on + Popup（须在 HUD Bind 之后 Emit）
- HP 满→未满 → Persist off；未满→满血 → Persist on + Popup
- 满血期间格挡伤害 0 **不**重播头顶
- `WarriorCombatDead` + `PushMapAdvanceView` 死亡表现 → `ClearAll`
- 同 SkillId 脚下只保留 1 个

## 不做

- Defend 接线
- 治疗回满以外的 Persist 触发源（当前无治疗通道则开战满血为唯一 on）

## 验收

- [x] 开战满血持有 Skill_02 的兵脚下有图标且头顶飘一次
- [x] 受伤收起；死亡立即清空头顶+脚下
- [x] D-071 持续条款可手验；勾 issue；INDEX SI-02→done

## 依赖

- [SI-01](01-overhead-popup.md)（done）
