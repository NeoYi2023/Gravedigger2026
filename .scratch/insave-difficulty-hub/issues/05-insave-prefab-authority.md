---
title: UI-029 InSaveShellPanel 权威 Prefab + MetaShellRoot Instance
status: ready for handcheck
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_04 §6（进档难度 Hub）
  - SPEC_03 §3.8 D-081
approach: A
---

## 目标

Play 所见进档壳与权威 Prefab `Assets/Prefabs/Meta/InSaveShellPanel.prefab` 一致：`MetaShellRoot` 仅嵌套 Prefab Instance，Ensure 不反向覆盖、不拆手调难度栏。

## 前置

1. Unity 跑菜单：`Gravedigger2026/Meta/Ensure InSaveShellPanel (UI-029)`
2. Console 应出现：`authoritative Prefab soft-patched` / `nests Prefab Instance`

## 验收

- [ ] `MetaShellRoot` 下 `InSaveShellPanel` 为 Prefab Instance（可开进权威 Prefab）
- [ ] 改权威 Prefab（如难度栏贴图/布局）→ 再进 Play → 所见一致（无需再改 MetaShellRoot 嵌死副本）
- [ ] 再跑 Ensure **不**把难度栏拆掉重建（已有 ColumnsScroll+DescriptionText）
- [ ] 进档 / 工具「关卡」仍打开 DifficultySelectHost；普通可进 UI-031

## 实现备注

- `MetaShellAssetBuilder.EnsureInSaveShellPanelOnExistingPrefab`
- Changelog v0.83.90
