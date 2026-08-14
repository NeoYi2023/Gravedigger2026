---
title: Dig HUD GM 矿灯手验
status: done
difficulty: 1
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.8 D-060
  - SPEC_03 §3.10 Demo GM
  - SPEC_04 §6 Dig Demo GM
selected_approach: A — DigHudView 增「获得矿灯」「划入矿灯升级」；TryAcquire / TrySpendCommonExp(1)
---

## 目标

Dig HUD 最小 GM 发放/划入矿灯；日志 Level / Exp / Q4·Q5·Q6 加成。

## 验收

- [x] 「获得矿灯」→ TryAcquire("Equip_MinerLamp")
- [x] 「划入矿灯升级」→ TrySpendCommonExp(..., 1)
- [x] 日志含 GraveSpawnWeightBonus Q4/Q5/Q6
- [x] D-060 手验可勾

## 依赖

- PE-07
