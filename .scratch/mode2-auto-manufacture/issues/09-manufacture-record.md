---
title: Mode2 UM 制造记录弹窗（最近一批只读摘要）
status: done
difficulty: 2
demo_scope: in-scope
selected_approach: A — AutoManufactureBatchRecordService + Mode2 Modal
spec_refs:
  - SPEC_03 §3.15 ManufactureRecord / AutoManufactureBatchRecord
  - SPEC_03 §3.11 Mode2 差分
  - SPEC_03 §3.6 UI-015
  - SPEC_03 §3.8 D-054
  - SPEC_04 §6 AutoManufactureBatch Prefs
---

## 目标

Mode2 UM 在「布阵」右侧提供「制造记录」入口；弹窗只读展示最近一批 AutoManufacture 士兵摘要。

## 范围

- `AutoManufactureBatchRecordService`：按槽 + CampaignMode 持久化最近一批 `WarriorId[]`；批末 Replace（含空批）
- Mode2 Prefab：`ManufactureRecordButton` + Modal；Mode1 无入口
- 列表：`WarriorName｜种族展示名｜ClassName`；池中缺失 Id 跳过；全空 →「本批无士兵」
- MetaShell 进档 Bind / 回档 Clear / 删档 Delete

## 不做

- Mode1 UI
- 再造 / 点行详情 / 多批历史
- 改 AutoManufacture 选料算法

## 验收

- [x] 代码路径：AM 批末 `Replace(flushedIds)`（含空批）→ Prefs `…AutoManufactureBatch`
- [x] 代码路径：Mode2 `EnsureManufactureRecordUi` / UmAssetBuilder Mode2 追加按钮+Modal；Mode1 不 Ensure
- [x] 代码路径：弹窗摘要 `WarriorName｜种族｜ClassName`；池缺失跳过；全空「本批无士兵」
- [x] 代码路径：进档 Bind / 回档 Clear / 删档 Delete 两模式键
- [ ] Unity 手验：Mode2 Dig→AM→UM 点「制造记录」可见本批名单
- [ ] Unity 手验：0 兵弹窗「本批无士兵」
- [ ] Unity 手验：再 Dig→AM 名单被新一批覆盖
- [ ] Unity 手验：退出再进同一 Mode2 档仍能看到上一批
- [ ] Unity 手验：Mode1 UM 无此按钮

## 依赖

- AM-07 Mode2 UM Prefab 分叉

## 编码前

- 难度 2：方案比选后编码 → **选定方案 A**（2026-08-12）

## 实现摘要

- `Core/AutoManufacture/AutoManufactureBatchRecordService` + SaveData
- `AutoManufactureStageModule` 批末 Replace；`MetaShellController` Bind/Clear/Delete
- `ManufactureRecordModalView` + `UpgradePanelView` Mode2 Ensure；`UmAssetBuilder` Regen `v0780` Mode2 追加
- 打开 Unity 后若 Prefab 未含按钮：运行时 Ensure 仍会生成；或菜单 `Gravedigger2026/UpgradeManufacture/Generate UM Prefabs + Catalog`
