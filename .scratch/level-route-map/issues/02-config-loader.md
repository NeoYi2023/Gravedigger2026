---
title: LRM-02 配置表扩列 + 加载器 + Snapshot
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_04 §9.1
  - SPEC_04 §9.31
  - SPEC_04 §14.7
  - SPEC_03 §3.9
approach: B
---

## 目标

Excel（Mode1+Mode2）增列并 Bake；C# Row / `ConfigCsvRepository` / `LevelRouteSnapshot` 贯通。

## 验收

- [x] Excel 三行表头扩列；已 Bake Mode1/Mode2 CSV（§14.7）
- [x] `LevelOperationConfigRow` / `SubLevelConfigRow` 有新字段
- [x] 加载器解析；同 LevelId 取首个非空 `RouteMapAssetId`
- [x] `BuildRouteSnapshot` 输出底图 Id 与坐标
