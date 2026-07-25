---
title: Meta 壳 — 存档三槽 / 工具面板 / 玩法三态占位
status: todo
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.4
  - SPEC_03 §3.5
  - SPEC_03 §3.6 UI-001～UI-006
  - SPEC_03 §3.7
  - SPEC_03 §3.8 D-001～D-004
  - SPEC_04 §6
---

## 目标

落地现行最小 Demo 外围壳，使进档后可识别玩法三态占位。

## 范围

- 存档选择：3 槽新建 / 进入 / 删除（含确认）
- 进档壳：浮动「工具」、ToolsPanel（设置/关卡占位）
- 默认 `GameplayState = Dig` 占位；三态可识别

## 不做

- 真实关卡加载、三玩法完整规则
- 完整存档 schema（超出槽占用）

## 验收

- [ ] D-001～D-004 可手验

## 依赖

- 建议与 [00-expand-demo-scope](00-expand-demo-scope.md) 同步或先完成 00（若要立刻接玩法切片）
- 负责人优先级：**本片先于玩法编码**
