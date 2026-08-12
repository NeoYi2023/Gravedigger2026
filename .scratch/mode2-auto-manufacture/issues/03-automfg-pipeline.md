---
title: AutoManufacture 阶段模块 + 选料/职业/属性循环 + 临时仓库
status: done
difficulty: 3
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.15 单兵流水线 / 选料 / 职业 / 基础属性
  - SPEC_03 §3.9 AutoManufacture 阶段结束
  - SPEC_03 §3.8 D-050 D-051
  - SPEC_04 §6 AutoManufacture stage
selected_approach: A — 独立 AutoManufactureService + 薄 StageModule（不改 Mode1 ManufactureService 主路径）
---

## 目标

实现 `GameplayType=AutoManufacture` 阶段：按 SPEC 循环选料→职业→基础属性→扣料→入临时仓库，直至不能再造。

## 范围

- `AutoManufactureStageModule` + 规则 Service
- 最低配方闸门；近似品质；主要手/次要手；其余部位优先级
- ClassRestrict 交集/回退；不计 Spirit/Control；不写 SoulId
- 临时仓库批内缓冲；余料留普通仓库
- 材料不足时 0 兵仍可结束阶段（上阵片处理清空）

## 不做

- 外观定稿 / 入 WarriorPool（AM-05）
- 自动上阵（AM-06）
- 魔法书效果（AM-04 空钩子可先留接口）

## 验收

- [x] Mode2 样例关卡 Dig 后进入 AutoManufacture（Mode2 `Level_01` 已插阶段行；或 Debug 推进）
- [x] 仓库材料足够时造出 ≥1 兵入临时仓库并扣料（规则层；手验：Dig 有料或调用 `DebugGrantMinRecipeKit`）
- [x] 无主要手/不足配方时停造并打日志
- [x] 阶段无玩家确认自动交还驱动（上阵 stub→AM-06；下一帧 `TryAdvanceStage`）

## 依赖

- AM-02

## 编码前

- 难度 3：须拆；本片仅流水线核心；方案比选后编码
- **选定方案 A**（2026-08-11）：独立 Service + 薄 StageModule

## 实现摘要

- `GameplayState.AutoManufacture=4`；`ConfigCsvRepository` / `LevelOperationDriver` 忽略 GameplayConfigId
- `Core/AutoManufacture/`：`AutoManufactureService`、`TempWarriorWarehouse`、`AutoCraftDraft`、`ISoldierManufactureMagicBookHook`（NoOp）
- `AutoManufactureStageModule`：Enter 跑批 → 延迟一帧交还；不 Instantiate Prefab
- `MetaShellController` 注册模块；Mode2 CSV `Level_01`：Dig→AutoManufacture→UM→PushMap
- Mode2 Excel 运作表未同步（运行时读 CSV；Excel 交 AM-08 手验时对齐）
- D-050 / D-051 → 部分实现（SPEC_00 v0.77.1）
