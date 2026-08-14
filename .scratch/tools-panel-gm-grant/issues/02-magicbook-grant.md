---
title: ToolsPanel 增加魔法书 TryEquip
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.5
  - SPEC_03 §3.6 UI-019
  - SPEC_03 §3.8 D-061
  - SPEC_03 §3.15
  - SPEC_04 §6
  - SPEC_04 §9.24
selected_approach: A — 复用 GmGrantListPanel；TryEquip 空槽
---

## 目标

ToolsPanel「增加魔法书」打开同一 `GmGrantListPanel`；点一次 `TryEquip` 装入第一个空槽。

## 范围

- `ToolsPanelView` 增「增加魔法书」按钮与事件
- `MetaShellController`：`configs.MagicBooks` 全表（DisplayName，空则 Id）→ `SpecialEquipSlotsService.TryEquip`
- 唯一已装 / 槽满 → Toast + 现有日志；列表保持打开
- Builder 重排含本按钮；不新建第二份 Overlay

## 不做

- 魔法书仓库 / 数量堆叠
- 正式装配 UI
- 删除 Dig HUD「装备战士强化」
- 改 `TryEquip` 契约

## 验收

- [x] 列表含当前模式全部 MagicBook（至少「还原」「战士强化」）
- [x] 点可叠书 → 装入空槽；连点至 6 槽满 → Toast 失败
- [x] 点 `IsUnique=1` 已装书 → Toast 失败
- [x] D-061 手验可勾

## 依赖

- TP-01

## 编码前

- 方案 **A** 已锁（INDEX）；可直接编码

## 完成备注

- ToolsPanel「增加魔法书」复用同一 `GmGrantListPanel`；`TryEquip`；槽满/唯一 Toast
