---
title: PushMap — SPEC close WarriorCombat DamagePopup HitFlash
status: done
difficulty: 3
demo_scope: authorized
approach: B
spec_refs:
  - SPEC_03 §3.14 Demo WarriorCombat / DamagePopup / HitFlash
  - SPEC_04 §6 PM-11～PM-13
  - SPEC_04 §9.22 WarriorCombat / DamagePopup / HitFlash 运行时契约
  - SPEC_00 v0.75.0
---

## 目标

关闭 PushMap「士兵 HP / 真实命中后置」「DemoKill 定时秒杀」「攻击仅动画」边界；录入方案 B（Defend 级 WarriorCombat + 飘字 + 闪烁）规则与运行时契约；拆分 PM-12/13 编码切片。

## 范围

- SPEC_03 §3.14 中英 Demo 边界更新
- SPEC_04 §6 / §9.22 中英契约
- SPEC_00 Changelog v0.75.0
- CONTEXT 术语 `DamagePopup` / `HitFlash`
- `.scratch/push-map` issues + NewAgent 话述

## 不做

- 任何 Unity / C# 游戏代码
- Defend 飘字/闪烁

## 验收

- [x] SPEC 双语边界与契约可审
- [x] Changelog v0.75.0
- [x] issues PM-11～13 + INDEX + new-agent-prompts

## 依赖

- 无（PM-01～10 done）

## 本会话交付

- 文档 only（本 Agent 不编程）
