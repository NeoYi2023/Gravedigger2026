---
title: 怪物走跑步态 — SPEC 约定关合
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12
  - SPEC_04 §9.19
  - SPEC_04 §15.5
  - SPEC_00 Changelog v0.83.35
  - CONTEXT MonsterConfig / WarriorAnimView
selected_approach: A — 共享 MonsterMoveGait；Agent Tick；AnimView 接收 useRun
---

## 目标

把怪物走/跑速度、走转跑计时、重置条件写入 SPEC；不新增 D-xxx。

## 锁定

- `MoveSpeed` = 走速；`RunSpeed` = 跑速（缺/≤0 → 走速）
- `WalkToRunSeconds` 缺 → 0.5；0 = 一开跑
- 离开移动即重置；下次从走起
- 有效移速 = gaitSpeed × Aggro 倍率 × 减速

## 验收

- [x] SPEC_03 §3.12 中英
- [x] SPEC_04 §9.19 / §15.5 中英
- [x] CONTEXT 术语
- [x] SPEC_00 Changelog v0.83.35
