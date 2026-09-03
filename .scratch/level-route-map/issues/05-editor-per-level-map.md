---
title: LRM-05 Editor + 每关 LevelRouteMap Prefab
status: ready for handcheck
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.9
  - SPEC_03 §3.6 UI-031
  - SPEC_03 §3.8 D-086
  - SPEC_04 §2
  - SPEC_04 §13
approach: C
---

## 目标

落地 `Assets/Prefabs/Level/LevelRouteMap_{LevelId}.prefab`（Demo：`Level_01`/`02`/`03`）：底图 + 子节点钉点（名=`GameplayOptionId`，`anchoredPosition`=卡片中心）。Editor 菜单从运作表+子关卡表同步缺钉、警告表外多余钉；Ensure 用 `RouteMapAssetId` 贴 Background。

## 前置

- LRM-04 done（SPEC 方案 C）

## 验收

- [x] 菜单可 Ensure / Sync `LevelRouteMap_Level_01`～`03`
- [x] Prefab 模式可拖动钉点；坐标约定：左下 `(0,0)`、Y 向上、宽 1450
- [x] 首次迁移：用现 CSV `MapPosX`/`MapPosY` 填 `anchoredPosition`（避免手摆丢失）
- [x] 表有、Prefab 无 → 自动补钉；Prefab 有、表无 → Warning
- [x] 子节点**不**做成完整选项卡（仅钉点 Transform / 占位）

## Unity 手验

1. `Gravedigger2026/Level/Ensure LevelRouteMap Prefabs (UI-031)`
2. 打开 `Assets/Prefabs/Level/LevelRouteMap_Level_01.prefab`：Background 有底图；钉点可拖；原点左下

## 实现备注

- `Assets/Editor/Level/LevelRouteMapAssetBuilder.cs`
- 读 Mode2 CSV：`Level_LevelOperationConfig` + `Level_SubLevelConfig`
- 不改运行时 View（LRM-06）
