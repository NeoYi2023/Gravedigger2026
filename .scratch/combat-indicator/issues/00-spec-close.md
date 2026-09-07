---
title: CI-00 SPEC close CombatIndicator UI-033 / D-089
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.6 UI-033
  - SPEC_03 §3.8 D-089
  - SPEC_03 §3.14
  - SPEC_03 §3.19
  - SPEC_04 §2
  - SPEC_04 §6
  - SPEC_04 §9.9b
  - SPEC_04 §9.19
  - SPEC_04 §13
  - SPEC_00 Changelog v0.84.19
approach: A
---

# 00 — SPEC close（战斗指示器）

## 目标

锁定 UI-033 / D-089 规则（方案 A：共享 Prefab + 0.2s Snapshot 轮询）；双语 SPEC + CONTEXT + Changelog。

## 验收

- [x] SPEC_03 术语 `CombatIndicator`；§3.6 UI-033；§3.8 D-089；§3.14 / §3.19 Demo 边界
- [x] SPEC_04 §2 Resources 路径；§6 实现意图；§9.9b / §9.19 `SilhouetteIconAssetId` + 内存 `TableOrder`；§13 Prefab 路径
- [x] CONTEXT + SPEC_00 Changelog **v0.84.19**
- [x] 选定方案 A 记录在 issues / plan

## 备注

本会话 **issues_only**：不写游戏代码。下一会话从 **01** 起。
