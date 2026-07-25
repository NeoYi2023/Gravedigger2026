---
title: Meta 壳 — 存档三槽 / 工具面板 / 玩法三态占位
status: done
difficulty: 2
demo_scope: in-scope
selected_approach: A
spec_refs:
  - SPEC_03 §3.4
  - SPEC_03 §3.5
  - SPEC_03 §3.6 UI-001～UI-006
  - SPEC_03 §3.7
  - SPEC_03 §3.8 D-001～D-004（流水线验收 D-010+ 见后续片）
  - SPEC_04 §6
---

## 目标

落地最小 Demo 外围壳（D-001～D-004），使进档后可识别玩法三态占位。流水线扩大验收已由 [00](00-expand-demo-scope.md) 写入；本片不实现玩法。

## 范围

- 存档选择：3 槽新建 / 进入 / 删除（含确认）
- 进档壳：浮动「工具」、ToolsPanel（设置/关卡占位）
- 默认 `GameplayState = Dig` 占位；三态可识别

## 不做

- 真实关卡加载、三玩法完整规则
- 完整存档 schema（超出槽占用）

## 实现摘要（方案 A）

- 持久化：`PlayerPrefs` 键 `Gravedigger2026.SaveSlot.{0|1|2}.Occupied`
- 场景：`Assets/Scenes/Boot.unity`（由 Editor 菜单/首次打开自动生成）
- Prefab：`Assets/Prefabs/Meta/MetaShellRoot.prefab`
- 脚本：`Assets/Scripts/Core|Meta|UI/`；生成器：`Assets/Editor/Meta/MetaShellAssetBuilder.cs`
- 工具设置/关卡：Toast；D-004 用手验 Debug「切下一态」（非正式壳层切态）

## 验收

- [x] D-001～D-004 可手验（打开 Unity 后若无 Prefab/Boot，等自动生成或菜单 `Gravedigger2026/Meta/Generate Meta Shell Prefabs + Boot Scene`，再 Play Boot）

## 依赖

- 建议与 [00-expand-demo-scope](00-expand-demo-scope.md) 同步或先完成 00（若要立刻接玩法切片）
- 负责人优先级：**本片先于玩法编码**
