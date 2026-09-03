---
title: LRM-03 Prefab/View 竖图坐标布局 + Resources
status: ready for handcheck
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.6 UI-031
  - SPEC_03 §3.8 D-086
  - SPEC_04 §2
  - SPEC_04 §6
approach: B
---

## 目标

`LevelRouteSelectRoot`：有底图时竖滑 MapContent（宽 1450）+ OptionsHost 绝对坐标；无底图回退 Stage 行。

## 前置（Unity）

1. `Gravedigger2026/Level/Ensure Route Map Resources (UI-031)`
2. `Gravedigger2026/Level/Ensure LevelRouteSelectRoot Prefab (UI-031)`

## 验收

- [ ] 普通进路线可见竖图；宽 1450、高按比例
- [ ] 选项钉在配置坐标；可竖滑；点可选项仍进玩法
- [ ] 无 `RouteMapAssetId` 关卡仍旧布局
- [ ] 缺图 Warning + 纯色 Content 仍可点

## 实现备注

- `LevelRouteSelectView` / `RuntimeFactory` / `AssetBuilder`
- Resources：`Assets/Resources/UI/SubLevelMaps/SubLevel_001`
- Mode2 样例坐标在 `Level_SubLevelConfig` `MapPosX`/`MapPosY`
