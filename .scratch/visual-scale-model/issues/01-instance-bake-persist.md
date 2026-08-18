---
title: WarriorInstance.VisualModelScale 烘印与 WarriorPool JSON
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.15 6b
  - SPEC_03 §3.8 D-066
  - SPEC_04 §6 WarriorPool
  - SPEC_04 §9.9 VisualModelScale
selected_approach: A — 独立字段 VisualModelScale；Bake 识别 Style_ScaleModel 后 return 不改材质赢家
---

## 目标

士兵池快照可读写 `VisualModelScale`；Mode2 Token **命中**放大 Style 时连乘 k，不抢 AllIn1 `VisualStyleId`。

## 范围

- 新增 `WarriorVisualModelScale`（Core）：`StyleId` / `StyleIdAlias` / `IsScaleStyle` / `Resolve`（≤0 → 1）
- `WarriorInstance.VisualModelScale` 默认 `1f`
- `WarriorVisualStyleBake.TryApply`：放大 Id → `VisualModelScale *= k`（k=`VisualIntensityAdd`，≤0 视为 1）后 **return**
- `WarriorSaveDto.VisualModelScale`；`WarriorPoolService` ToDto/FromDto；旧档缺/≤0 → 1
- `RepairMissingStatSnapshots` **不得**清空 `VisualModelScale`
- `RefinalizeInstance` 已不碰该字段，确认保持

## 不做

- View Scale / Catalog Kind（VS-02）
- BodyRadius / AttackRange / 布阵占位（VS-03）
- 改现有 7 本 Demo 书的 AllIn1 列

## 验收

- [x] 命中 `Style_ScaleModel` Add=1.5 → 实例 `VisualModelScale==1.5`，`VisualStyleId` 仍可被另一本材质书占用
- [x] 两本 1.5 → `2.25`
- [x] 进档往返；旧档缺字段加载为 1
- [x] 勾 issue；回复变更清单

## 依赖

- VS-00

## 编码前

- 方案 **A** 已锁；可直接编码
