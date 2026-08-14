---
title: ProtagonistEquipmentService 入账/升级/公共经验/持久化
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.16
  - SPEC_03 §3.8 D-059
  - SPEC_04 §6
  - SPEC_04 §9.25
selected_approach: A — 纯 C# Service + PlayerPrefs；镜像 SpecialEquipSlotsService Bind/Clear/Delete
---

## 目标

实现存档级装备仓规则：首次获得、同 Id 转化、连升、满级转公共池、`SpendCommonExp`；按 `SaveSlot+CampaignMode` 读写。

## 范围

- 新增 `ProtagonistEquipmentService`（建议路径 `Assets/Scripts/Core/ProtagonistEquipment/` 或 `Core/UpgradeManufacture/`）
- API 至少：`BindSlot` / `ClearBound` / `DeleteSlotData` / `TryAcquire(EquipId)` / `TrySpendCommonExp(EquipId, amount)` / 只读 `OwnedEquips` / `EquipCommonExp`
- 键名：`…EquipCommonExp`、`…ProtagonistEquipmentWarehouse`（SPEC_04 §6）
- `MetaShellController` 进档 Bind / 回档 Clear / 删档双模式清键（对齐 SpecialEquipSlots）

## 不做

- Dig caps 重算接线（PE-03）
- GM / 正式 UI
- 改材料 Warehouse

## 验收

- [x] 首次 Acquire → Level=1 CurrentExp=0
- [x] 再 Acquire 同 Id → CurrentExp += ConvertExpValue，可连升
- [x] 满级再 Acquire → EquipCommonExp 增加
- [x] SpendCommonExp 扣池、升等级
- [x] 进档/回档/删档 PlayerPrefs 行为正确

## 依赖

- PE-01

## 编码前

- 方案 **A** 已锁；可直接编码

## 完成备注

- 路径：`Assets/Scripts/Core/ProtagonistEquipment/`（`OwnedEquip` / `ProtagonistEquipmentSaveData` / `ProtagonistEquipmentService`）
- Prefs 后缀：`SaveSlotPrefsKeys.EquipCommonExpSuffix` / `ProtagonistEquipmentWarehouseSuffix`
- MetaShell：进档 Bind、回档 Clear、删档双模式 `DeleteSlotData`
- Dig caps 重算留 `Changed` 事件给 PE-03；本片不接线
