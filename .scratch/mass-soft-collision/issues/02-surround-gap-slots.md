---
title: Surround 缺口 AttackSlot（追怪不挤点）
status: done
difficulty: 3
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12 方案 B+ CombatMoveMode=Surround / SurroundGap
  - SPEC_04 §9.7 B+ 运行时契约
---

## 目标

扩展 `AttackSlotService.TryClaim` 支持 `SurroundParams`：环上认领时跳过 `SurroundGapDegrees` 扇区，使多近战围同一目标时留出缺口，不再实心环挤成一团。

## 范围

- 扩展 `Assets/Scripts/Core/Pathing/AttackSlotService.cs`
- `SurroundGapDirection` + `GapDegrees`（Demo 默认 60）
- 默认缺口方向：相对「目标←进攻方质心」的背侧扇区
- 近战多打一默认传 surround；远程默认不传（Chase）
- 同一目标多名认领者角度不落入缺口；缺口外仍可正常认领
- 正确性自检（角度分布 / 缺口 / 释放再认领）

## 不做

- SoftCollision（→ SC-01）
- Sweep
- Follow

## 验收

- [x] 纯 C# 可测；无 Stage 接线亦可验证几何（`AttackSlotCorrectnessChecks.RunAll` 新增三检；已脱机 mono 编译运行全过）
- [x] 不破坏既有无 surround 调用（MP-02 语义）（原四检零改动通过；surround 为可选末参，缺省 null）

## 落地记录

- 选定方案：**A — TryClaim 内过滤 + 质心滚动维护**（难度 3 已确认）
- 新建：`Assets/Scripts/Core/Pathing/SurroundParams.cs`（`SurroundGapDirection` + `SurroundParams`，`Default`=Bottom/60°）
- 改动：`AttackSlotService.cs`（可选末参 `SurroundParams? surround`；`TargetSlotTable` 滚动质心；`IsSlotInGap`/`TryComputeGapCenterDegrees`；`TryGetGapCenterDegrees` 调试探针）；`AttackSlotCorrectnessChecks.cs`（+Gap/Top/ReleaseReclaim 三检）；`MassPathingPerfStressMenu.cs`（+自检菜单入口）
- 语义澄清：方向映射 Bottom=背侧（默认）/Top=朝进攻方/Left·Right=绕逼近轴 ±90°/Random=targetId 哈希调试角；空表以首认领者 `attackerPos` 播种逼近轴；边界角步严格落入半宽内才跳过；缺口覆盖全环 → 认领返回 false
- SPEC_04 §9.7 方向映射与质心语义已澄清；SPEC_00 Changelog v0.74.4

## 依赖

- MP-02 `AttackSlotService`
- [00](00-spec-close.md)
