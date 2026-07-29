---
title: Defend Prepare 接入共享编辑器并清理旧 FormationPanelView
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12 Prepare
  - SPEC_03 §3.8 D-040
  - SPEC_03 §3.6 UI-009
---

## 目标

Defend Prepare 使用同一 `FormationEditorRoot`（复用本阶段地图）；「开战」关闭编辑器并正式部署；隐藏/移除旧列表布阵 UI。

## 验收

- [x] 进 Defend 即拖拽布阵编辑器
- [x] ≥1 可开战；开战后 Combat 正常
- [x] UM / Defend 写同一 BattleFormation

## 依赖

- [02](02-soldier-bar-drag.md)
