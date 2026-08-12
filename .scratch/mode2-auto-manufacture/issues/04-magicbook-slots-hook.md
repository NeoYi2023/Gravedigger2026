---
title: 魔法书 6 槽存档结构 + SoldierManufacture 空钩子
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.15 魔法书
  - SPEC_04 §9.24 MagicBookConfig
  - SPEC_04 §6 SpecialEquipSlots
selected_approach: A — SpecialEquipSlotsService + SoldierManufactureMagicBookHook（对齐 WarriorPool 绑定模式）
---

## 目标

主角 6 特殊装备槽持久化 + AutoManufacture 制造环节空钩子（无具体效果）。

## 范围

- PlayerPrefs `SpecialEquipSlots` JSON（按 CampaignMode）
- 装配闸门：唯一书不可叠（规则 API；可不做 UI）
- 造兵时对已装备且 `EffectPhase` 含 `SoldierManufacture` 的书调用空钩子

## 不做

- 装备/卸下 UI
- 具体 EffectPayload 数值变更
- Combat 环节触发

## 验收

- [x] 进档/切模式可读写 6 槽（`BindSlot` + `TryEquip`/`TryUnequip` → PlayerPrefs；缺表行 Demo 仍可写）
- [x] 造兵路径调用钩子（有匹配书时日志 `[MagicBook] SoldierManufacture empty hook…`）；无书时跳过

## 依赖

- AM-02（可与 AM-03 并行接口约定）

## 编码前

- 难度 2：方案比选后编码
- **选定方案 A**（2026-08-11）

## 实现摘要

- `SaveSlotPrefsKeys.SpecialEquipSlotsSuffix`
- `SpecialEquipSlotsSaveData` / `SpecialEquipSlotsService`（Bind/Clear/Delete + TryEquip/At/Unequip + IsUnique 闸门）
- `SoldierManufactureMagicBookHook`（实装；保留 NoOp）
- `MetaShellController`：进档 Bind、回档 ClearBound、删档 DeleteSlotData；注入 AutoManufacture
- SPEC_00 v0.77.2；SPEC_04 §6/§9.24；D-051 备注含 AM-04
