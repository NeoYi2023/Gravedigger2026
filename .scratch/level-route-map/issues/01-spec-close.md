---
title: LRM-01 SPEC 关闭（路线底图 + MapPos）
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
approach: B
---

## 目标

将 UI-031 关卡路线底图（宽 1450、高按比例）与子关卡选项坐标写入 SPEC/CONTEXT/Changelog。

## 验收

- [x] SPEC_03：RouteSelect 术语、UI-031、§3.9 字段表、D-086
- [x] SPEC_04：§2 Resources 例外；§9.1 `RouteMapAssetId`；§9.31 `MapPosX`/`MapPosY`；关卡驱动句
- [x] CONTEXT `RouteSelect`；SPEC_00 Changelog v0.83.83
