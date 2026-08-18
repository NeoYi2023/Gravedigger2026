---
title: InSaveShell 装备/魔法书入口按钮 + 弹窗壳
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.5
  - SPEC_03 §3.6 UI-022
  - SPEC_03 §3.6 UI-023
  - SPEC_03 §3.8 D-067
  - SPEC_03 §3.8 D-068
  - SPEC_04 §6
selected_approach: A — MetaShellAssetBuilder 外科补丁；两 Modal 空壳
---

## 目标

在 `InSaveShellPanel` 左下 `BackButton` 正上方增加「装备」「魔法书」按钮；点击打开画面居中 Modal 壳（遮罩 + 标题 + 关闭），内容区留空待 EM-02/03 填充。

## 范围

- [`MetaShellAssetBuilder`](Gravedigger2026/Assets/Editor/Meta/MetaShellAssetBuilder.cs)：
  - `BuildInSaveShell`：在 `BackButton` 上方竖排创建 `EquipmentButton`（文案「装备」）、`MagicBookButton`（文案「魔法书」）；`BackButton` 下移；同宽 160×48，间距 8
  - 新增 `BuildEquipmentWarehousePanel` / `BuildMagicBookSlotsPanel`（布局对齐 `LevelSelectPanel` / `GmGrantListPanel`：全屏 dim + 中框 + Title + Close；魔法书壳预留 `BookRowHost` 空位）
  - 菜单 `Gravedigger2026/Meta/Ensure InSaveEquipMagicBookPanels (UI-022/023)`：`Ensure*OnExistingPrefab` 外科补丁现有 `MetaShellRoot`，**禁止**整表重建
- [`InSaveShellView`](Gravedigger2026/Assets/Scripts/UI/InSaveShellView.cs)：SerializeField + 事件 `EquipmentRequested` / `MagicBookRequested`；`Show/HideEquipmentWarehousePanel` / `Show/HideMagicBookSlotsPanel`；`Hide()` 时一并关面板
- [`MetaShellController`](Gravedigger2026/Assets/Scripts/Meta/MetaShellController.cs)：订阅上述事件 → 打开/关闭对应面板（EM-01 仅空壳，无列表/拖拽逻辑）
- 更新 [`MetaShellRoot`](Gravedigger2026/Assets/Prefabs/Meta/MetaShellRoot.prefab)（跑 Ensure 菜单或 Builder）

布局参考（左下锚点）：

```
[装备]     y = 24 + 48 + 8 + 48 + 8 = 136  （相对 BackButton 上方第二格）
[魔法书]   y = 24 + 48 + 8 = 80
[返回存档] y = 24
```

## 不做

- 装备列表内容（EM-02）
- BookRow / TrySwap / 拖拽（EM-03）
- 删改 Tools GM / Dig HUD GM

## 验收

- [x] Play Mode 进档 → 左下可见「装备」「魔法书」「返回存档」三按钮，顺序正确
- [x] 点「装备」→ 居中 Modal 打开，标题含「装备」，关闭可关
- [x] 点「魔法书」→ 居中 Modal 打开，标题含「魔法书」，关闭可关
- [x] 两 Modal 互斥或可同时关；回存档选择时面板关闭
- [x] Ensure 菜单可在已有 MetaShellRoot 上补丁，不丢现有引用
- [x] INDEX EM-01→done

## 依赖

- EM-00（SPEC 已关）

## 编码前

- 方案 **A** 已在 INDEX 锁定；EM-00 done 后可直接编码
