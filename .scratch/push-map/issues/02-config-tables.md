---
title: PushMap — 配置表 Bake 与 CSV 加载
status: todo
difficulty: 2
demo_scope: deferred
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

- [ ] Bake 产出 CSV；运行时可只读加载
- [ ] AggroMode 四值可解析

## 依赖

- [00](00-spec-close.md)

## 编码前

- 可与 PM-01 并行；难度 2 方案比选；须 Demo 授权
