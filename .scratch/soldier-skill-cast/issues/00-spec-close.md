---
title: 士兵战斗施放 — SPEC 规则关闭 + D-069
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12 WarriorCombat / SkillCast
  - SPEC_03 §3.11 士兵技能 / 失控二次判定
  - SPEC_04 §9.21 SkillConfig
  - SPEC_04 §9.21b SkillEffectConfig
  - SPEC_04 §9.7 Defend/PushMap 战斗接线
  - SPEC_03 §3.8 D-069
selected_approach: C — 占用普攻通道 3×方案 D；首片 PushMap；进距且 CD 好即放
---

## 目标

关闭「Demo 士兵可在战斗中施放技能」的规则与验收框架；修订现行「Demo 不施放」边界。

## 锁定

- 验收号 **D-069**（D-067 已用于装备仓只读）
- 首片战场：**PushMap**
- 插入点：CD 好且已进距即放（不打断进行中前摇/已射弹道）
- 通道：占用普攻通道，连发走 3 次命中方案 D

## 验收

- [x] §3.12 / §9.21b 已写入施放语义（中英）
- [x] D-069 已写入 §3.8（中英）
- [x] Changelog 已记；勾 issue；INDEX SC-00→done
