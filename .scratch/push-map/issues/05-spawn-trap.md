---
title: PushMap — 刷怪点与陷阱触发
status: todo
difficulty: 3
demo_scope: deferred
spec_refs:
  - SPEC_03 §3.14 刷怪
  - SPEC_04 §9.23 PushMapSpawnConfig
---

## 目标

无陷阱：开战且关联目标未占领 → 刷怪；有陷阱：忠诚士兵首次进入 TrapZone → 刷怪；占领后停新刷，已刷怪保留。

## 范围

- 一点多 MonsterId 行；SpawnOrder
- **不**使用 WaveSpawn 倒计时

## 不做

- AggroMode 四态完整（PM-06 可先用 Defend 默认追击）
- BOSS 通关结算（PM-07）

## 验收

- [ ] 无陷阱开战刷可验
- [ ] 陷阱触发一次可验
- [ ] 占领后不再新刷；场上怪仍在

## 依赖

- [01](01-map-prefab-markers.md)
- [02](02-config-tables.md)
- [04](04-objective-capture.md)

## 编码前

- 难度 3 方案比选；须 Demo 授权
