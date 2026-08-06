---
title: PushMap — SPEC 规则录入收尾确认
status: done
difficulty: 2
demo_scope: deferred
spec_refs:
  - SPEC_03 §3.14 PushMap
  - SPEC_04 §9.22 PushMapGameplayConfig
  - SPEC_04 §9.23 PushMapSpawnConfig
  - SPEC_04 §9.19 MonsterConfig.AggroMode / AlertRadius
  - CONTEXT PushMap terms
  - SPEC_00 v0.52.0
---

## 目标

关闭推图战规则库框架（非 Unity 编码）。

## 范围

- SPEC_03 §3.14 双语骨架 + 边界锁定
- SPEC_04 表结构与地图标记契约
- CONTEXT / Changelog / SPEC_02 / spec-map 同步

## 不做

- Unity C# / Prefab / Excel 实文件
- 扩大 §3.8 Demo 验收项

## 验收

- [x] §3.14 / §9.22–§9.23 / AggroMode 已写入
- [x] A3 边界已锁定并记入待澄清清单

## 依赖

- 无

## 编码前

- 本片无编码；后续片须负责人授权 Demo
