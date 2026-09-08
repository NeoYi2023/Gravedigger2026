---
title: SE-CAM-01 常量键 + HoldFramingSolver
status: done
difficulty: 2
demo_scope: out-of-scope
spec_refs:
  - SPEC_03 §3.19 HoldFraming
  - SPEC_04 §9.20b SearchExtractHold*
  - SPEC_04 §14.7 ConfigTableDualSync
approach: B
depends_on:
  - SE-CAM-00
---

## 目标

落地 HoldFraming 纯计算与表驱动常量；**尚不**接 Stage 开/关。

## 范围

- Mode2 `Combat_CombatConstantConfig` Excel 增 `SearchExtractHold*` 键（保留三行表头）→ Bake CSV
- `CombatConstantKeys` + `CameraPresentationConstants` 读入
- 新建 `Assets/Scripts/Gameplay/SearchExtract/SearchExtractHoldFramingSolver.cs`（纯 C#）：
  - 输入：相机、Objective XZ、忠诚兵世界位列表、当前 Size/look-at、常量
  - 输出：目标 look-at + 目标 Size
  - 视口 AABB；外框立刻扩大；内框滞留后才收紧；半径钳制；Objective bias
- 可选：Editor/单元可调用的静态盒测（无 Play Mode）

## 不做

- `SearchExtractStageController` / `PushMapCameraFollowController` 接线（SE-CAM-02）
- 改 PushMap 默认跟随

## 验收

- [x] Excel+CSV 双格式同步；键可经 `GetCombatConstantOrFallback` 读到
- [x] Solver 在给定假数据下：越外框 → Size↑；全在内框满滞留 → Size 才↓
- [x] 圈外兵不参与包围盒；look-at 不超出 `HoldMaxPanRadius`

## 落地

- Mode1+Mode2 Excel 在 `PushMapCameraIntroWaypointDwellSeconds` 后插入 11 键（三行表头保留）并 Bake
- `SearchExtractHoldFramingConstants` 挂在 `CameraPresentationConstants.HoldFraming`
- Solver：`SearchExtractHoldFramingSolver.cs`；盒测菜单 `Gravedigger2026/SearchExtract/Run HoldFraming Solver Checks (SE-CAM-01)`

## 依赖

- SE-CAM-00
