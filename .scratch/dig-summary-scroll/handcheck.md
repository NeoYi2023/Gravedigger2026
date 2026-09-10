# DigStageSummary 三行可视 + 竖滑 — 手验清单

**SPEC:** UI-011 / v0.84.39  
**Prefab:** `Assets/Prefabs/Dig/DigStageRoot.prefab` → `SummaryRoot/Body`

## 结构核对（已完成，无需 Play Mode）

- [x] `Body` 920×620 挂 `ScrollRect`（vertical / Clamped / sensitivity 28）
- [x] `Body/Viewport` 有 `RectMask2D`
- [x] `Body/Viewport/Content` 有 `GridLayoutGroup` FixedColumnCount=5 + `ContentSizeFitter` vertical Preferred
- [x] `DigStageSummaryView._itemGrid` → `Content`

## Play Mode（回 Unity 后）

1. Dig 挖到 **≤15** 种汇总项 → 格子全在约三行内、无明显空滑
2. Dig / GM 造出 **≥16** 种 → 只露约三行、可上下拖/滚轮、溢出被 Mask 裁切
3. 空账本 → 「本阶段未获得奖励。」；右上「X」确认仍交还关卡驱动

可选：菜单 `Gravedigger2026/Dig/Ensure Dig Summary Item Grid (UI-011)` 再跑一次对齐 Editor 重建路径。
