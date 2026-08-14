---
title: GmGrantListPanel + ToolsPanel 增加主角装备
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.5
  - SPEC_03 §3.6 UI-019
  - SPEC_03 §3.8 D-061
  - SPEC_04 §6
selected_approach: A — 克隆 LevelSelectPanel；一份 GmGrantListPanel；TryAcquire
---

## 目标

ToolsPanel「增加主角装备」打开通用发放列表；点一次获得该 EquipId 1 个（`TryAcquire`）。

## 范围

- 新建 `GmGrantListPanelView`（克隆 `LevelSelectPanelView`：滚动按钮行 + 关闭；`Show(title, items)`）
- `ToolsPanelView` 增「增加主角装备」按钮与事件
- `InSaveShellView` 转发
- `MetaShellController`：当前模式 `ProtagonistEquipmentRows` 按 EquipId 去重（Level 1 DisplayName）→ `TryAcquire` → Toast
- `MetaShellAssetBuilder`：加高 ToolsPanel 并重排；菜单 `Gravedigger2026/Meta/Ensure GmGrantListPanel (UI-019)` 手术补 Prefab
- 列表保持打开可连点；失败 Toast 现有 error

## 不做

- 「增加魔法书」入口与 `TryEquip`（TP-02）
- 正式装备仓 UI
- 删除 Dig HUD GM
- 改 `TryAcquire` 契约

## 验收

- [x] Play Mode 进档 → 工具 →「增加主角装备」列出当前模式去重装备（至少铁铲、矿灯）
- [x] 点一次未持有 → 入仓 L1；再点同 Id → 转化经验（日志/Toast 可观察）
- [x] 关闭按钮可关面板；ToolsPanel 加高后五项可点（本片魔法书按钮可暂缺）
- [x] Ensure 菜单可把 GmGrantListPanel 补到现有 MetaShell Prefab

## 依赖

- TP-00

## 编码前

- 方案 **A** 已锁（INDEX）；可直接编码

## 完成备注

- `GmGrantListPanelView` + ToolsPanel 两项入口 + `TryAcquire` / Editor Ensure
- 运行时若 Prefab 未补：克隆关卡按钮 / 克隆 LevelSelectPanel 作为发放列表
