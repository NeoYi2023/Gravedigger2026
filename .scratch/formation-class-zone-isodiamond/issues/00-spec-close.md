---
title: SPEC 关闭职业区 IsoDiamond
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.8 D-052
  - SPEC_03 §3.15 自动上阵
  - SPEC_04 §6 AutoManufacture
  - SPEC_04 §13 FormationClassZone
  - SPEC_00 v0.82.15
selected_approach: A — WalkSurface 同形 IsoDiamond
---

## 目标

把 FormationClassZone 从带 IsoTileYaw 的 XZ OBB 改写为与 WalkSurface 相同的 IsoDiamond 空间划定，供后续切片编码。

## 范围

- SPEC_03 术语 / D-052 / §3.15 区域与放置
- SPEC_04 §6 / §13 契约（MeshCollider 作者向；Play 关闭；不含 RotationYDegrees）
- CONTEXT `FormationClassZone` / `IsoTileYaw` / `IsoDiamond`
- Changelog v0.82.15

## 不做

- Unity C# / Prefab（FZ-01 / FZ-02）

## 验收

- [x] 双语 SPEC 已写 IsoDiamond；废止 IsoTileYaw/OBB
- [x] Changelog 已记

## 依赖

- 无

## 编码前

- 本片无编码
