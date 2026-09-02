---
title: TF-00 战术阵型 SPEC 关闭（方案 A）
status: done
difficulty: 3
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.18 TacticalFormation
  - SPEC_03 §3.12 MassCombatPathing / B+ Follow 口径修订
  - SPEC_03 §3.8 D-084
  - SPEC_04 §9.30 TacticalFormationConfig
  - SPEC_04 §9.24 GrantFormationSkill
  - SPEC_04 §9.7 FormationSlot 运行时契约
  - SPEC_00 v0.83.61
approach: A
---

## 目标

锁定战术阵型（TacticalFormation）规则：**虚拟阵型单元 + 布阵组阵覆盖职业区**；产出 SPEC / issues，本会话不编码。

## 范围

- SPEC_03 §3.18（双语）+ §3.12 B+「不做 Follow」例外口径 + D-084
- SPEC_04 §9.30 表 + §9.24 Token + §9.7 契约 + §9.21 `FormationId` + §13 Pattern Prefab
- CONTEXT / spec-map / SPEC_00 Changelog v0.83.61
- `.scratch/tactical-formation/` TF-00～TF-06 issues

## 不做

- 任何 C# / Prefab / Excel·CSV 改动（后续切片）

## 验收

- [x] 方案 A 写入 SPEC 并双语同步
- [x] 与 FormationBond / BattleFormation 术语区分已写明
- [x] issues 可独立 Agent 接手

## 依赖

- 无
