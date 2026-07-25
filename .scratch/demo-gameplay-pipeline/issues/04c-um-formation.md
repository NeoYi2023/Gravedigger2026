---
title: UpgradeManufacture — 布阵区写 BattleFormation
status: todo
difficulty: 2
demo_scope: planned
spec_refs:
  - SPEC_03 §3.11 战斗布阵
  - SPEC_03 §3.12 Prepare 布阵编辑
  - SPEC_03 §3.6 UI-010
  - SPEC_04 §13 Prefabs/Maps（布阵预览若挂地图，用 Ground_* 池）
---

## 目标

同屏布阵区：连续坐标上阵/下阵/改位置；写回与 Defend Prepare 共用的 BattleFormation；底部「完成」结束本阶段。

## 范围

- 简陋布阵 UI；控制力占用展示（可粗）
- 与制造区士兵池打通
- 坐标系：BattleMap 连续坐标；若本片需要地面预览，用 `Prefabs/Maps/Ground_*`（与 Dig/Defend 同池），勿另造 `BattleMap_*` 命名

## 不做

- Defend 开战与战斗

## 验收

- [ ] 至少 1 名士兵可上阵并持久到下一阶段可读
- [ ] 「完成」触发阶段结束（§3.9）

## 依赖

- [04b](04b-um-manufacture.md)
