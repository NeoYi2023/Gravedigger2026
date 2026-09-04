---
title: DWH-02 Dig HUD 统计 UI + 聚合接线
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.10
  - SPEC_04 §6 Dig 垂直切片
  - SPEC_04 §9.34
approach: A
---

## 目标

`DigWarehouseHudStatsBuilder` 聚合；`DigHudView` 三行图标+Hover Tips；`DigStageController` / `DigAssetBuilder` 接线。

## 验收

- [x] 精魂 / 残骸（非主要手）/ 种族·职业主要手统计正确；0 隐藏
- [x] 图标 60×60、字号 24、间距 10、行距 20
- [x] Hover Tips Key=`DigWarehouseHoverTips`
- [x] 废止 raw Id 文本摘要刷新

## 备注

图标路径 `Assets/Art/UI/Icons/`；Editor 回退 `AssetDatabase.LoadAssetAtPath`。
