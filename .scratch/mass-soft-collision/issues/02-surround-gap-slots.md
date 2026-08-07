# SC-02 — Surround 缺口 AttackSlot

**状态：** todo  
**难度：** 3  
**依赖：** SC-00；扩展 MP-02 `AttackSlotService`

## 目标

`TryClaim(..., surround?: SurroundParams)`：环上跳过 `SurroundGapDegrees` 扇区。

## 范围

- `SurroundGapDirection` + `GapDegrees`（Demo 默认 60）
- 默认缺口：相对「目标←进攻方质心」背侧扇区
- 近战多打一默认 Surround；远程默认不传 surround（Chase）
- 正确性：同目标多认领者角度不落入缺口；缺口外仍可认领

## 不做

- SoftCollision（→ SC-01）
- Sweep
- Follow

## 验收

- 纯 C# 可测；无 Stage 接线亦可验收几何
