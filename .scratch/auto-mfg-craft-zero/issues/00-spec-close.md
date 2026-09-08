---
title: AM-CRAFT-00 SPEC 关账（0 兵诊断）
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.8 D-051
  - SPEC_03 §3.10 WarehouseHudStats
  - SPEC_03 §3.15
  - SPEC_04 §6
  - SPEC_00 v0.84.32
approach: C
---

## 目标

锁定 0 兵诊断约定与已知缺口：HUD 主要手数 ≠ 可造套数；最高主要手失败即整批停造。

## 范围

- SPEC_03 / SPEC_04 / Changelog 双语
- 发布 AM-CRAFT-01～03

## 不做

- 改选料算法（AM-CRAFT-02/03）

## 验收

- [x] §3.15 Demo 诊断日志 + 已知缺口已写
- [x] issues INDEX 已建
