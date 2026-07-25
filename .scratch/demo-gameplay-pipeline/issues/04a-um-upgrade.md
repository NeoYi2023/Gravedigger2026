---
title: UpgradeManufacture — 升级区（经验 / 等级表）
status: todo
difficulty: 2
demo_scope: planned
spec_refs:
  - SPEC_03 §3.11 升级
  - SPEC_04 §9.8 ProtagonistLevelConfig
  - SPEC_03 §3.6 UI-010
---

## 目标

升级区可读表展示等级与上限；支持生涯经验达标连升（经验可先 Debug 注入，正式入账在 05d）。

## 范围

- 读 `ProtagonistLevelConfig`；应用 TechPoints / ControlPowerCap / ProtagonistMaxHP（护盾上限语义）
- 简陋升级区 UI（细则控件不 polish）

## 不做

- 制造、布阵、科技树画布

## 验收

- [ ] 注入经验后可连升并看到表字段生效

## 依赖

- [00](00-expand-demo-scope.md)；建议 [03](03-dig-vertical.md) 之后便于联调资源流
