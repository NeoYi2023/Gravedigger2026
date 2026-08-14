---
title: Step2 脉冲回调套书 + Deploy 延后
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.15 §9 / §11 UI-016
  - SPEC_03 §3.8 D-055
selected_approach: A — pulse-peak callback + deferred DeployBatch
---

## 目标

演出 Step2 槽缩放到峰值时回调 Core apply；全部结束后再上阵；失败/Exit 兜底。

## 完成备注

- `Begin(..., onBookPulsePeak)`；`CoPulseBook` 峰值回调
- `RefreshFocusedCardClass` / `RefreshClass`
- StageModule：Enter 不上阵；完成/fallback/`Exit` 套剩余书后 Deploy
