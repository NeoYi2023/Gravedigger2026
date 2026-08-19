---
title: 战斗技能图标 — SPEC 规则关闭 + D-071
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12 SkillCast 表现
  - SPEC_03 §3.8 D-071 / UI-025
  - SPEC_03 §3.14 Demo CombatSkillIcon 边界
  - SPEC_04 §6 AllyFootCircle / CombatSkillIcon
  - SPEC_04 §9.22 CombatSkillIcon 运行时契约
selected_approach: A — 士兵子节点 SpriteRenderer + 正交相机像素换算
---

## 目标

关闭「PushMap 战斗技能图标」规则与验收框架（UI-025 / D-071）。

## 锁定

- 验收号 **D-071**
- 战场：**PushMap**；Defend **无**
- 头顶 35×35 静止 0.6s 后世界 +Z 上飘 0.3s 淡出；右排间距 4px
- 脚下持续 20×20；同 SkillId 只保留 1 个
- 规则事件 `SkillIconPopup` / `SkillPersistChanged`

## 验收

- [x] §3.12 / §3.14 / §9.22 已写入（中英）
- [x] D-071 / UI-025 已写入 §3.8（中英）
- [x] Changelog / CONTEXT / spec-map 已记；勾 issue；INDEX SI-00→done
