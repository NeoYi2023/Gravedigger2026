---
title: BodyRadius 与 AttackRange 按 k 缩放
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.15 6b
  - SPEC_03 §3.8 D-066
  - SPEC_04 §9.9 / CombatReach
selected_approach: A — BodyRadius 与 ClassConfig.AttackRange 均 ×k
---

## 目标

放大士兵的碰撞、寻路半径与攻击距离与视觉一致。

## 范围

- Defend / PushMap spawn Bind：`bodyRadius *= k`（从而 `NavMeshAgent.radius`）
- `DefendSessionService.TryRegisterWarrior` 与 PushMap 对等处：`AttackRange *= k`
- PushMap 传入 `Bind` 的本地 `attackRange` 同样 ×k
- `AutoFormationDeployService.ResolveBodyRadius` 乘 k（签名改为吃 `WarriorInstance`）
- 手验：Mode2 Excel `MagicBook_Restore` 填 `VisualStyleId=Style_ScaleModel`、`VisualIntensityAdd=1.5`，Bake 后重新造兵。勿覆盖战士强化/进阶的 AllIn1 列

## 不做

- 改 ClassConfig 表内 AttackRange 原文
- UI 卡缩放

## 验收

- [x] k=1.5 士兵布阵占位更疏、NavMesh 半径更大、挥刀距离随 CombatReach 变远
- [x] 士兵卡 / 底栏缩略图尺寸不变
- [x] 勾 issue；回复变更清单

## 依赖

- VS-02

## 编码前

- 方案 **A** 已锁；可直接编码
