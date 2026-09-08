---
title: AM-CRAFT-02 跳过无法成套的最高主要手
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.15 自动选择躯体材料
  - SPEC_03 §3.8 D-051
  - SPEC_00 v0.84.34
approach: A
---

## 目标

最高 BodyLevel 主要手凑不齐近似品质套件时，跳过该主要手（不消耗）并试下一档；全部无法成套才停造。

## 依赖

- AM-CRAFT-01 Console 确认锚点 Lv 与库存 Lv 错配

## 不做

- 次要手改用第二只主要手（AM-CRAFT-03）
- 放宽 `|ΔBodyLevel|≤1`

## 验收

- [x] 代码：`TryCraftOne` 按 BodyLevel 降序尝试主要手，失败 Skip 不扣料
- [ ] Play Mode：Dig_01（全 Lv1）+ Dig_02（含 Lv3 主要手）混合库存仍能用 Lv1 套件造兵
- [ ] 仍优先用能成套的最高主要手
