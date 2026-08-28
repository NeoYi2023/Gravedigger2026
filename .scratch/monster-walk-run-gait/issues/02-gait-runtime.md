---
title: 怪物走跑步态 — 运行时与动画
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12
  - SPEC_04 §15.5
selected_approach: A
---

## 目标

`MonsterMoveGait` + `WarriorAnimView.SetMoving(…, useRun)` + Defend/PushMap Agent 速度与动画对齐 SPEC。

## 验收

- [x] 移动 0.5s 后切跑（速度+Run 动画）
- [x] 攻击/死亡/进距/受堵/击晕后重置，再移动从走起
- [x] Defend 与 PushMap 共用 gait 逻辑
- [x] 士兵行为不变
