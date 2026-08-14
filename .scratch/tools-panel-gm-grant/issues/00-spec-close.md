---
title: SPEC 关闭 ToolsPanel GM 发放入口
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.5
  - SPEC_03 §3.6 UI-003
  - SPEC_03 §3.8（本片补 D-061）
  - SPEC_04 §6
  - SPEC_00
selected_approach: A — GmGrantListPanel Overlay；装备 TryAcquire；魔法书 TryEquip
---

## 目标

把 ToolsPanel Demo GM「增加主角装备」「增加魔法书」写入 SPEC，供后续切片编码。

## 范围

- SPEC_03 §3.5 本期条目 + 交互（点击入口 → 关 ToolsPanel → GmGrantListPanel）
- SPEC_03 §3.6 UI-003 扩写；新增 **UI-019** GmGrantListPanel
- SPEC_03 §3.8 新增 **D-061**（P1）
- SPEC_04 §6 ToolsPanel Demo GM 一句
- SPEC_00 Changelog bump（v0.82.17）
- 中英双块同步；CONTEXT 无需新术语

规则要点：

- 列表按钮：`DisplayName`，空则回退 Id
- 装备：当前模式表按 EquipId 去重（取 Level 1 DisplayName）；点一次 `TryAcquire`
- 魔法书：当前模式 `MagicBookConfig` 全表；点一次 `TryEquip`（无仓库）
- 成功/失败 Toast + 日志；列表保持打开可连点
- Dig HUD 现有 GM 保留

## 不做

- Unity C# / Prefab（TP-01 / TP-02）

## 验收

- [x] §3.5 / §3.6 UI-019 / §3.8 D-061 中英已写
- [x] SPEC_04 §6 已记 ToolsPanel GM
- [x] Changelog 已记

## 依赖

- 无

## 编码前

- 本片无编码
