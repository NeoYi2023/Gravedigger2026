---
title: PushMap — 配置表 Bake 与 CSV 加载
status: done
difficulty: 2
demo_scope: authorized
selected_approach: A
spec_refs:
  - SPEC_04 §9.22 PushMapGameplayConfig
  - SPEC_04 §9.23 PushMapSpawnConfig
  - SPEC_04 §9.19 AggroMode / AlertRadius
  - SPEC_04 §14 Bake Tables
---

## 目标

新增 Excel/CSV：`PushMap_PushMapGameplayConfig`、`PushMap_PushMapSpawnConfig`；`MonsterConfig` 增 AggroMode/AlertRadius；Bake + `ConfigCsvRepository` 可加载。

## 范围

- 四段 Excel 名 + 两段 CSV 名（§14）
- 样例至少 1 个 GameplayConfigId + 多 Spawn 行（含陷阱/无陷阱/BOSS）

## 不做

- StageModule 接线（PM-03）
- AI / 占领逻辑

## 验收

- [x] Bake 产出 CSV；运行时可只读加载
- [x] AggroMode 四值可解析

## 依赖

- [00](00-spec-close.md)

## 落地摘要（方案 A）

- Excel：`推图战_推图战配置表_PushMap_PushMapGameplayConfig.xlsx`、`推图战_刷怪配置表_PushMap_PushMapSpawnConfig.xlsx`；Monster 表增列后重写
- CSV：`PushMap_PushMapGameplayConfig.csv`、`PushMap_PushMapSpawnConfig.csv`；`Defend_MonsterConfig.csv` 含四值 AggroMode
- 代码：`AggroMode` 枚举、`PushMap*ConfigRow`、`ConfigCsvRepository` 加载/`TryGetPushMap`/`GetPushMapSpawnRows`
- SPEC：v0.55.0（§9.19 缺省 + §6 PM-02）

## 编码前

- 可与 PM-01 并行；难度 2 方案比选；须 Demo 授权
