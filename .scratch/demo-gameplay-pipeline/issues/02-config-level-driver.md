---
title: 配置表加载与关卡阶段驱动骨架
status: todo
difficulty: 2
demo_scope: planned
spec_refs:
  - SPEC_03 §3.9
  - SPEC_04 §9.1 LevelOperationConfig
  - SPEC_04 §9.2 DigMapId
  - SPEC_04 §9.7 BattleMapId
  - SPEC_04 §13 Prefabs/Maps
  - SPEC_04 §14
---

## 目标

运行时只读 `ConfigTables/Csv/`；按 `LevelOperationConfig` 升序驱动阶段；进入/离开钩子可挂各玩法模块。

## 范围

- CSV 加载（至少 Level + Dig/Defend 玩法键，含 `DigMapId` / `BattleMapId`）
- 阶段切换：Dig / UpgradeManufacture / Defend 占位或模块入口
- UM 阶段按 00 关闭的 ConfigId 语义处理（勿误读 Dig 表）
- 地图 Id 合法值校验：`Ground_01`…`Ground_05`；解析约定 → `Assets/Prefabs/Maps/{Id}.prefab`（本片可只解析/日志，Instantiate 交给 03/05a）

## 不做

- 各玩法完整逻辑（交给 03+）
- Editor 打表工具（可另开）

## 验收

- [ ] 能按样例关卡推进阶段，UI/日志显示 LevelId、StageNumber、GameplayType
- [ ] Dig/Defend 的 GameplayConfigId 能解析到对应表行，并读出 DigMapId / BattleMapId

## 依赖

- [00](00-expand-demo-scope.md)、[01](01-meta-shell.md)
