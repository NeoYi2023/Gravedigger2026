---
title: AM-RAIN-02 尸骸雨 + 法阵闪红
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.15 StepA
  - SPEC_04 §6 / §13 AmBodyPartPiece
depends_on:
  - AM-RAIN-01
---

## 目标

独立 2D 物理演出层：池化落下本批 SourceItemIds；弱碰撞堆叠；法阵置于堆下可持续闪红。

## 验收

- [x] `AmBodyPartPiece` + `AmBodyPartPool`（运行时 Ensure Prefab）
- [x] 每 0.3s 随机 3～5；总数不变
- [x] MagicCircle_1 闪红（`AmMagicCircleView`）
- [x] 落地 Sleep；Layer AmBody/AmSoldier/AmFloor
