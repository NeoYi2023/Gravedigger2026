---
title: SPEC 关闭 InSaveShell 装备仓 / 魔法书排序 UI
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.5
  - SPEC_03 §3.6
  - SPEC_03 §3.8
  - SPEC_03 §3.15
  - SPEC_03 §3.16
  - SPEC_04 §6
  - SPEC_00
selected_approach: A — 共享 BookRow Prefab；TrySwap + Changed；装备仓只读
---

## 目标

把进档壳「装备」「魔法书」正式 UI 写入 SPEC，供 EM-01～03 编码。

## 范围

- **SPEC_03 §3.5** InSaveShell 本期条目：左下 `BackButton` 正上方增「装备」「魔法书」；点击打开对应居中 Modal；Tools GM（UI-019）保留
- **SPEC_03 §3.6** 新增 **UI-022**（主角装备仓弹窗，只读）、**UI-023**（魔法书 6 槽 + 左键拖拽排序）；扩写 UI-003 若需
- **SPEC_03 §3.8** 新增 **D-067**（装备仓只读列表）、**D-068**（魔法书拖拽互换 + AM BookRow 同步）（P1）
- **SPEC_03 §3.15** 删除「本轮不做装备/卸下 UI」；锁：槽下标 0→5 = 左→右启动顺序；任意两槽 `TrySwap`（含空槽）；无独立魔法书仓库；本轮不卸下
- **SPEC_03 §3.16** 正式仓 UI 本轮只读 `OwnedEquip`（名/等级/描述/图标）；仓内拥有即生效
- **SPEC_04 §6** Meta 壳：`EquipmentWarehousePanel` / `MagicBookSlotsPanel`；`SpecialEquipSlotsService.TrySwap` + `Changed`；`Assets/Prefabs/AutoManufacture/BookRow.prefab` 共享
- **SPEC_00** Changelog bump；**spec-map.md** 增 UI-022/023 行；CONTEXT 视需要补一句
- 中英双块同步

规则要点：

- 入口布局：上「装备」、中「魔法书」、下「返回存档」；各 160×48；间距 8；Mode1/Mode2 均显示
- 弹窗：对齐 UI-008（全屏遮罩 + 中框 + 关闭）；`sortingOrder` ≥ 100 盖住 AM 演出
- 装备列表：`ProtagonistEquipmentService.OwnedEquips` + 当前等级配置行；空态「尚未拥有装备」
- 魔法书：嵌套 AM `BookRow`；Swap 后立即 persist；AM 演出 BookRow 订阅 `Changed` 同步

## 不做

- Unity C# / Prefab（EM-01～03）
- 装备升级 / 公共经验划入 UI
- 魔法书卸下 / 从弹窗装入（仍 GM `TryEquip`）

## 验收

- [x] §3.5 / §3.6 UI-022 / UI-023 / §3.8 D-067 / D-068 中英已写
- [x] §3.15 排序规则与 §3.16 只读仓 UI 已写
- [x] SPEC_04 §6 + Changelog + spec-map 已记
- [x] INDEX EM-00→done

## 依赖

- 无

## 编码前

- 本片无编码
