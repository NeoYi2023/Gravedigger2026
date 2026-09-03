---
title: LRM-07 地图模式 Icon-only + 悬停 Tips
status: ready for handcheck
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.6 UI-031
  - SPEC_03 §3.8 D-086
  - SPEC_03 §3.9
  - SPEC_04 §6
  - SPEC_04 §9.31
approach: A
---

## 目标

`LevelRouteMap` 地图模式下，每个 `GameplayOptionId` 场景**仅显示 Icon**；Type / Title / Description / Reward 放入壳层悬停 Tips（`OptionHoverTips`）。旧 Stage 行布局仍显示完整选项卡。

## 前置

- LRM-06 done（运行时钉点已落地）

## 验收

- [x] SPEC_03 UI-031 / D-086 / §3.9 + SPEC_04 + Changelog v0.83.88（双语）
- [x] 地图模式：选项约 80×80，隐藏 Type/Title/Description/Reward
- [x] 悬停显示 Tips；移开 / Rebuild / Hide 关闭 Tips
- [x] 可选项仍可点击；锁定态可悬停不可点
- [x] Stage 行布局仍完整选项卡
- [x] Ensure / RuntimeFactory 可建 `OptionHoverTips`；Prefab 已注入

## Unity 手验

1. Mode2 普通进路线（有 `LevelRouteMap_{LevelId}`）：地图上只见 Icon
2. 鼠标指向 Icon → Tips 出现 Type / Title / Description / Reward
3. 移开鼠标 → Tips 消失
4. 可点 Icon 仍能进玩法；锁定 Icon 可悬停、不可点
5. 无 `RouteMapAssetId` / 无地图 Prefab 的关卡：Stage 行仍完整卡

## 实现备注

- `LevelRouteSelectView`：地图分支 Icon-only + `LevelRouteOptionHover`
- 壳层 `Box/OptionHoverTips`（默认 inactive）
- 菜单：`Gravedigger2026/Level/Ensure LevelRouteSelectRoot Prefab (UI-031)`
- 离线注入：`.scratch/tools/lrm07_option_hover_tips.py`
