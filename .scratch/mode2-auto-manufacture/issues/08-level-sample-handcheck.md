---
title: Mode2 样例关卡运作行 + D-050～D-053 手验清单
status: done
difficulty: 1
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.8 D-050～D-053
  - SPEC_03 §3.9
  - SPEC_03 §3.15
  - SPEC_04 §14 Mode2 LevelOperation
---

## 目标

Mode2 样例 `Level_LevelOperationConfig` 插入 AutoManufacture 阶段；完成 §3.8 D-050～D-053 手验并回写状态。

## 范围

- Mode2 运作表：Dig → AutoManufacture → UpgradeManufacture →（战斗阶段保持既有）
- 手验清单文档化（本 issue）
- 通过后将 D-050～D-053 状态改为已实现（方案注记）

## 不做

- 新玩法规则变更

## 验收

- [x] Mode2 CSV/Excel `Level_01`：`Dig → AutoManufacture → UpgradeManufacture → PushMap`（Excel 已与 CSV 对齐）
- [x] Mode1 运作表**无** `AutoManufacture`（静态校验通过）
- [x] D-050～D-053 手验清单已文档化（见下；负责人 Play Mode 按步勾选）
- [x] SPEC_03 §3.8 D-050 / D-051 → 已实现；D-052 / D-053 注记确认

## 依赖

- AM-06
- AM-07

## 编码前

- 难度 1：可直接改表 + 手验（负责人确认 2026-08-11）

## 实现摘要

- Mode2 CSV（AM-03）已含 AutoManufacture；本片用 `.scratch/tools/am08_sync_mode2_level_operation.py` 将 Mode2 Excel 与 CSV 对齐，并更新表头备注含 `AutoManufacture`
- 静态校验：Mode2 Excel==CSV；Mode1 无 AutoManufacture；`Level_01` 流水线锁定
- 手验清单见下；Changelog SPEC_00 v0.77.6

---

## D-050～D-053 手验清单（Play Mode）

前置：Unity Editor 打开工程；Console 无配置加载失败；选 **Mode2** 进档。

### D-050 — 运作流水线 + 自动交还

| # | 步骤 | 期望 | 勾选 |
|---|------|------|------|
| 1 | Tools「关卡」启动样例 `Level_01` | 日志/UI：`LevelId=Level_01`，Stage1 `GameplayType=Dig` | [ ] |
| 2 | Dig 有效时长归零 → DigStageSummary 确认 | 进入 Stage2 `AutoManufacture`（无制造 UI Prefab 亦可） | [ ] |
| 3 | AutoManufacture 跑完 | **无**玩家确认；自动 `TryAdvanceStage` → Stage3 `UpgradeManufacture` | [ ] |

### D-051 — 自动造兵（最低配方）

| # | 步骤 | 期望 | 勾选 |
|---|------|------|------|
| 4 | Dig 攒够最低配方料（头+躯干+双臂含主要手+双腿），或 Debug 灌最低套 | Console：造出 ≥1 兵；扣料；不计 Spirit/Control | [ ] |
| 5 | 查士兵实例 | `SoulId` 空；`ControlPowerCost=0`；`AttackMode` 来自 ClassConfig；余料仍在仓库 | [ ] |
| 6 |（可选）无主要手/不足配方 | 停造 + 日志；Tips「无士兵可制造」约 1s；阶段仍可结束并进 UM | [ ] |

### D-052 — 清阵 + PlacementOrder / 职业区上阵

| # | 步骤 | 期望 | 勾选 |
|---|------|------|------|
| 7 | AutoManufacture 批结束瞬间 | 布阵先清空，再仅本批 Id 上阵 | [ ] |
| 8 | 打开 UM「布阵」 | 本批兵在对应 `FormationClassZone`；同区无严重重叠；旧池兵未自动再上 | [ ] |

### D-053 — Mode2 UM 差分

| # | 步骤 | 期望 | 勾选 |
|---|------|------|------|
| 9 | Mode2 UM 主屏 | **无**手动制造区；可见 GM 升级 / 完成 / 布阵 | [ ] |
| 10 | Mode2 布阵编辑器 | **无**控制力占用 HUD；仍可改阵 | [ ] |
| 11 |（回归）Mode1 同路径 | Mode1 UM 仍有手动制造；布阵仍有控制力 HUD | [ ] |

### 进战斗（验收主路径收尾）

| # | 步骤 | 期望 | 勾选 |
|---|------|------|------|
| 12 | Mode2 UM「完成」 | 进入 Stage4 `PushMap`（或既有战斗阶段）；可完成 Prepare→开战路径 | [ ] |

**备注：** Agent 本片完成表对齐 + 静态校验 + 清单文档；Play Mode 勾选由负责人在 Editor 执行。实现链路 AM-03～07 已接线，D-050/D-051 SPEC 状态随本片回写为已实现。
