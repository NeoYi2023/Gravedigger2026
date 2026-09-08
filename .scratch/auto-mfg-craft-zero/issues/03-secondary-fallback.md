---
title: AM-CRAFT-03 无副手时第二只主要手当次要手
status: todo
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.15 最低配方
  - SPEC_03 §3.15 次要手
---

## 目标

仅当 AM-CRAFT-01 日志证明 `ArmSecondary=0` 且槽位其余足够：近似档内没有 `IsPrimaryHand=0` 时，允许另一只 Arm（含主要手）当次要手，对齐「臂×2 须含 ≥1 主要手」。

## 依赖

- AM-CRAFT-02
- Console 证明缺副手而非混品质

## 不做

- 两只主要手都没有时仍须停造
