---
title: 士兵栏横滑 + 拖拽上阵/改位/下阵 + 控制力 HUD
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.11 战斗布阵
  - SPEC_03 §3.8 D-032
---

## 目标

底部 80×80 士兵栏（横滑）；拖出上阵；战场改位；回栏/地图外下阵；左上角 used/Cap；Idle 缩略图。

## 验收

- [x] 栏内横滑；拖出上阵写坐标
- [x] 改位 / 下阵符合 SPEC
- [x] 控制力 HUD 即时刷新

## 依赖

- [01](01-spec-service-um-skeleton.md)
