---
title: 主角装备仓只读弹窗（UI-022 / D-067）
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.5
  - SPEC_03 §3.6 UI-022
  - SPEC_03 §3.8 D-067
  - SPEC_03 §3.16
  - SPEC_04 §6
  - SPEC_04 §9.25
selected_approach: A — 滚动列表绑定 OwnedEquips + 当前等级行
---

## 目标

「装备」弹窗展示当前存档已拥有的全部主角装备（只读）：名称、等级、描述、图标（有则显）。

## 范围

- 新建 `Assets/Scripts/UI/EquipmentWarehousePanelView.cs`（或等价命名）：
  - `Show()` 时从 `ProtagonistEquipmentService.OwnedEquips` 刷新
  - 每行：`DisplayName`（空则 `EquipId`）+ `Lv.{Level}` + `Description`（当前等级 `ProtagonistEquipmentConfig` 行）
  - `IconAssetId` 非空 → `Resources.Load<Sprite>`
  - 空仓文案：「尚未拥有装备」
  - 订阅 `ProtagonistEquipmentService.Changed`（Tools GM 发放后列表自动更新）
- `InSaveShellView` / `MetaShellController`：打开装备弹窗时注入 Service + Configs
- `MetaShellAssetBuilder`：EquipmentWarehousePanel 内 Scroll + RowTemplate 接线（对齐 GmGrantListPanel 滚动结构）
- OnDisable/OnDestroy 取消订阅 `Changed`

## 不做

- 升级 / 划入 `EquipCommonExp` / 卸下
- 点击行发放装备（仍 Tools GM）
- 魔法书弹窗（EM-03）

## 验收

- [x] GM 发放铁铲/矿灯后，打开装备弹窗可见对应行与等级
- [x] 描述与配置表当前等级行一致
- [x] 空仓时显示「尚未拥有装备」
- [x] 弹窗打开期间 GM 再发放 → 列表刷新（或重开可见）
- [x] D-067 手验可勾
- [x] INDEX EM-02→done

## 依赖

- EM-01（弹窗壳 + 入口按钮）

## 编码前

- 方案 **A** 已锁；EM-01 done 后编码
