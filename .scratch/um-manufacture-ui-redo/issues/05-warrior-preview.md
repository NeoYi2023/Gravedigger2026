---
title: Warrior visual preview gate Attack then Idle
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.11 躯体外观可视预览
  - SPEC_04 §6 D-031
  - SPEC_04 §15.5 WarriorAnimView
---

## 目标

非宝石槽全满才显示试算外观；否则静态占位图；显示时 Attack 一遍再 Idle。

## 验收

- [ ] 闸门未满 → 占位图
- [ ] 闸门满 → 外观 + Attack→Idle（无 Animator 则静态降级）
