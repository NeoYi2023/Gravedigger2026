---
title: Mode2 UM 隐藏手动制造；保留升级与可编辑布阵；屏蔽控制力 HUD
status: done
difficulty: 2
demo_scope: in-scope
selected_approach: C — Prefab 分叉（Mode2 StageRoot / FormationEditorRoot）
spec_refs:
  - SPEC_03 §3.11 Mode2 差分
  - SPEC_03 §3.15
  - SPEC_03 §3.8 D-053
  - SPEC_04 §6（Mode2 Prefab Resolve）
---

## 目标

Mode2 进入 UpgradeManufacture 时关闭手动制造 UI；保留升级 Modal 与布阵编辑；隐藏控制力 HUD。

## 范围

- `CampaignMode.Mode2`：Instantiate `UpgradeManufactureStageRoot_Mode2`（ManufactureZone 默认关）
- 保留 GM升级、完成、布阵
- `FormationEditorRoot_Mode2`：控制力 HUD 默认关；Catalog `ResolveEditorRoot`；不按 ControlPower 拒上阵（既有）

## 不做

- 改 Mode1 行为
- AutoManufacture 算法本身

## 验收

- [x] Mode2 UM 看不到手动制造区，仍可升级与改阵
- [x] Mode1 UM 行为不变
- [x] Mode2 布阵无控制力占用显示

## 依赖

- AM-05（建议 AM-06 之后联调）

## 编码前

- 难度 2：方案比选后编码 → **选定方案 C**
