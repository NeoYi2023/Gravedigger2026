---
title: LRM-04 SPEC 闭合（每关地图 Prefab 钉点）
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.9
  - SPEC_03 §3.8 D-086
  - SPEC_03 §3.6 UI-031
  - SPEC_04 §2
  - SPEC_04 §9.1
  - SPEC_04 §9.31
  - SPEC_04 §6
approach: C
---

## 目标

将 UI-031 选项坐标权威从子关卡表 `MapPosX`/`MapPosY` 改为每关 Prefab `LevelRouteMap_{LevelId}`；写入 SPEC / CONTEXT / Changelog。

## 验收

- [x] SPEC_03：RouteSelect 术语、UI-031、§3.9（去掉表内坐标；写明 Prefab 钉点约定）、D-086
- [x] SPEC_04：§2 例外（钉点在 Prefab）；关卡驱动句；§9.1 `RouteMapAssetId` 语义；§9.31 删除 MapPos 列定义
- [x] CONTEXT `RouteSelect` / `LevelRouteMap`；SPEC_00 Changelog v0.83.84
- [x] INDEX 追加 LRM-04/05/06

## 备注

本片**不**改代码、不改 Excel。编码见 LRM-05 / LRM-06。
