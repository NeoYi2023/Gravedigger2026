---
title: 战斗技能图标 — 头顶飘（Skill_03 / Skill_01）
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.8 D-071
  - SPEC_03 §3.12 SkillCast 表现
  - SPEC_04 §9.22 CombatSkillIcon
selected_approach: A — WarriorSkillIconHudView + SkillIconHud.prefab；像素换算保持 35 屏幕像素
---

## 目标

PushMap 士兵头顶显示瞬时技能图标：Skill_03 提交成功、Skill_01 格挡成功。

## 接线

- `TryCommitSkillBurst` 成功 → `SkillIconPopup(id, Skill_03)`
- `TryRollSkill01Block` 成功 → `SkillIconPopup(id, Skill_01)`
- `PushMapStageController` 解析 `PushMapAdvanceView` → `WarriorSkillIconHudView.PlayPopup`
- 同兵未结束图标向屏幕右排（4px）；消失后左移靠齐
- `HitFlashView` / `WarriorAnimView` 跳过 `SkillIcon*` Renderer

## 验收

- [x] HUD + Prefab + Catalog 槽位
- [x] 35px 正交换算；变焦屏幕像素不变
- [x] Skill_03 / Skill_01 头顶飘
- [x] 右排与独立计时
