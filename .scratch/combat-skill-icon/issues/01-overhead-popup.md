---
title: 头顶瞬时技能图标（Skill_03 提交 / Skill_01 格挡）
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.8 D-071
  - SPEC_03 §3.12 SkillCast 表现
  - SPEC_04 §9.22 CombatSkillIcon
selected_approach: A — WarriorSkillIconHudView + SkillIconPopup
---

## 目标

PushMap 士兵瞬时技能在该兵头顶显示 35×35 图标：静止 0.6s 后沿世界 +Z 上飘 0.3s 淡出；未结束图标向屏幕右侧排开（间距 4px，消失后左移靠齐）。

## 范围

- `WarriorSkillIconHudView.PlayPopup` + `SkillIconHud.prefab`
- `PushMapSessionService.SkillIconPopup`：`TryCommitSkillBurst` 成功 → `Skill_03`；格挡成功 → `Skill_01`
- `PushMapStageController` 订阅并 Bind HUD；`HitFlashView` 跳过 `SkillIcon*` Renderer
- 变焦时屏幕像素不变（正交 `orthoSize / Screen.height`）
- 缺图空框；路径 `Resources/UI/Skills/{SkillId}`

## 不做

- `Skill_02` 脚下持续（→ SI-02）
- Defend 接线
- UI 技能栏 / 手动按键

## 验收

- [x] 法师 `Skill_03` 提交后该兵头顶飘 `Skill_03` 图标
- [x] `Skill_01` 格挡成功头顶飘；满血格挡伤害 0 **不**额外因 Skill_02 重播（SI-02 状态机）
- [x] 同兵多图标右排；消失后靠齐
- [x] D-071 头顶条款可手验；勾 issue；INDEX SI-01→done

## 依赖

- [SI-00](00-spec-close.md)（done）
