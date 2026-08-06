---
title: PushMap — StageModule 与 LevelOperation 接线
status: todo
difficulty: 3
demo_scope: deferred
spec_refs:
  - SPEC_03 §3.9 GameplayType PushMap
  - SPEC_03 §3.14 PushMapPhase / 复用边界
  - SPEC_03 §3.12 Prepare / Shield / LOC
---

## 目标

`PushMapStageModule`（或等价）识别 `GameplayType=PushMap`；复用 Prepare / FormationEditor / StartBattle / Shield / LossOfControl；按 MapId Instantiate 地图。

## 范围

- LevelOperation → PushMapGameplayConfig
- ModeSelect 模式2 正式路径可后置挂钩（Demo D-044 占位可保留）
- 开战 ≥1；护盾初值；失控 roll

## 不做

- 目标点占领、刷怪、AggroMode、BOSS 通关

## 验收

- [ ] 样例关卡阶段可进入 PushMap Prepare→Combat
- [ ] Shield / LOC 行为与 Defend 对齐可观察

## 依赖

- [02](02-config-tables.md)

## 编码前

- 难度 3：强制方案比选；本会话仅本片；须 Demo 授权
