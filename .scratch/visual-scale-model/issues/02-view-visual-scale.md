---
title: Catalog Kind=ScaleModel 与 Visual.localScale
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.15 6b
  - SPEC_04 §15.2
  - SPEC_03 §3.8 D-066
selected_approach: A — ApplyTo 在材质逻辑后设 Visual.localScale=(k,k,k)
---

## 目标

世界 Instantiate 后按 `VisualModelScale` 放大 Visual 子级；Catalog 登记 `Style_ScaleModel` 且无材质。

## 范围

- `WarriorVisualStyleCatalog.Entry` 增加 `Kind`（Material=0 / ScaleModel=1）
- `TryGet`：ScaleModel 允许 `Material==null`
- Catalog.asset 加一行 `StyleId=Style_ScaleModel`、Kind=ScaleModel
- `WarriorAllIn1StyleView.ApplyTo`：材质逻辑不变；再把 `Visual.localScale` 设为 `Vector3.one * Resolve(warrior)`
- 四处 Instantiate 已调用 ApplyTo，一般不必改调用点

## 不做

- UI-016 卡 / 底栏缩略图缩放
- BodyRadius / AttackRange（VS-03）
- 改 Prefab 根 Scale

## 验收

- [x] k=1.5 时 Visual 本地 XYZ 均为 1.5；根 Scale 不变
- [x] 无放大书时 Visual 保持 Prefab `(1,1,1)`
- [x] 勾 issue；回复变更清单

## 依赖

- VS-01

## 编码前

- 方案 **A** 已锁；可直接编码
