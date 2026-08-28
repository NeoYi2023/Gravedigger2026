---
title: 怪物走跑步态 — 配表与加载
status: done
difficulty: 1
demo_scope: in-scope
spec_refs:
  - SPEC_04 §9.19
  - SPEC_00 Changelog v0.83.35
selected_approach: A
---

## 目标

`Defend_MonsterConfig` 增 `RunSpeed`、`WalkToRunSeconds`；`MonsterConfigRow` + `ConfigCsvRepository` 加载缺省。

## 样例

- `WalkToRunSeconds=0.5` 全行
- `RunSpeed`：普通怪 1.0；Boss（Monster_11/12）0.6

## 验收

- [x] Csv + Mode2 Csv 列序正确
- [x] Excel 双模式同步（或 migrate 脚本字段注释）
- [x] 缺列/空/非法值按 SPEC 加载
- [x] `ResolveRunSpeed()` 回退走速
