---
title: 共享 BookRow + 魔法书拖拽排序（UI-023 / D-068）
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.5
  - SPEC_03 §3.6 UI-023
  - SPEC_03 §3.8 D-068
  - SPEC_03 §3.15
  - SPEC_04 §6
  - SPEC_04 §9.24
selected_approach: A — BookRow.prefab 共享；TrySwap + Changed；弹窗 AllowReorder
---

## 目标

「魔法书」弹窗展示与 AM 演出相同的 6 槽 `BookRow`；已装书显示在对应 `BookSlot`；左键拖拽与其他槽互换（含空槽）；Swap 后立即存档；`AutoManufacturePresentationRoot` 内 BookRow 同步更新。

## 范围

### 规则层

- [`SpecialEquipSlotsService`](Gravedigger2026/Assets/Scripts/Core/AutoManufacture/SpecialEquipSlotsService.cs)：
  - 新增 `TrySwap(int indexA, int indexB, out string error)`：交换两槽 `MagicBookId`（含一空）；越界 / 未绑档失败；成功 → `Persist()` + `Changed?.Invoke()`
  - 新增 `event Action Changed`（与 `ProtagonistEquipmentService` 同模式）

### 共享 BookRow Prefab（方案 A）

- 新建 `Assets/Prefabs/AutoManufacture/BookRow.prefab`（6×`BookSlot_0`…`_5`，120×160，`AutoMfgMagicBookSlotView`）
- [`AmAssetBuilder`](Gravedigger2026/Assets/Editor/AutoManufacture/AmAssetBuilder.cs) / [`AutoManufacturePresentationController.Build`](Gravedigger2026/Assets/Scripts/Gameplay/AutoManufacture/AutoManufacturePresentationController.cs)：
  - 抽出 `CreateBookSlot` 逻辑到共享 Prefab 或 Builder 先生成 BookRow 再 nested 进 `AutoManufacturePresentationRoot`
  - 演出根 `_bookSlots` 仍可从 nested instance 收集引用
- 弹窗 `MagicBookSlotsPanel`：`BookRowHost` 嵌套同一 Prefab

### 绑定与刷新

- 新建 `MagicBookSlotsPanelView`（或 `BookRowPresenter`）：
  - `Bind(SpecialEquipSlotsService, ConfigCsvRepository)` → 遍历 6 槽 `BindBook` / `BindEmpty`（复用 [`AutoMfgMagicBookSlotView`](Gravedigger2026/Assets/Scripts/Gameplay/AutoManufacture/AutoMfgMagicBookSlotView.cs) 逻辑）
  - 订阅 `SpecialEquipSlotsService.Changed` 刷新
- [`AutoManufacturePresentationController`](Gravedigger2026/Assets/Scripts/Gameplay/AutoManufacture/AutoManufacturePresentationController.cs)：
  - `BindBooks` 在绑档后订阅 `Changed`；`OnDisable` 取消
  - 弹窗 Swap 后 AM 演出中 BookRow 无需重进阶段即更新

### 拖拽（仅弹窗 `AllowReorder=true`）

- 新建 `MagicBookSlotDragHandler`（或扩 `AutoMfgMagicBookSlotView`）：
  - 左键 `IBeginDragHandler` / `IDragHandler` / `IEndDragHandler`
  - 拖动 ghost 跟随指针；落点另一槽 → `TrySwap`；否则回弹
  - 空槽可为目标（搬书）；演出 BookRow `AllowReorder=false` 不挂 handler

### Meta 接线

- `MetaShellController` 打开魔法书弹窗时传入 `_specialEquipSlots` + `_configs`

## 不做

- 魔法书卸下 / 从弹窗装入新书（仍 Tools GM `TryEquip`）
- 改 `TryEquip` / Unique 闸门语义
- 装备列表（EM-02 已完成部分不动）

## 验收

- [x] Tools GM 装入 ≥2 本魔法书 → 打开魔法书弹窗，槽位与 GM 顺序一致（左→右）
- [x] 拖拽 A↔B 两本 occupied 槽 → 顺序互换，重进档仍保持
- [x] 拖拽 occupied → empty → 书移到空槽，源槽空
- [x] AutoManufacture 演出 Step1 BookRow 与弹窗排序一致（Swap 后若演出已 Instantiate 则即时刷新）
- [x] Step2 单槽脉冲仍按新下标 0→5 左→右触发（手验 2 本顺序对调后效果顺序变化）
- [x] D-068 手验可勾
- [x] INDEX EM-03→done

## 依赖

- EM-01（魔法书弹窗壳 + BookRowHost）
- EM-02 可与 EM-03 并行，但 BookRow 拖拽依赖 EM-01 壳

## 编码前

- 方案 **A** 已锁；EM-01 done 后编码
