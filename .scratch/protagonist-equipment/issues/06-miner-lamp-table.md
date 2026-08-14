---
title: 矿灯配置表 5 级行
status: done
difficulty: 1
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.8 D-060
  - SPEC_03 §3.16
  - SPEC_04 §9.25
  - SPEC_04 §14
selected_approach: A — Mode1+Mode2 同步追加 Equip_MinerLamp L1～5；保留 Equip_IronShovel
---

## 目标

`Protagonist_ProtagonistEquipmentConfig` 追加矿灯 5 行（Mode1+Mode2 Excel+CSV）。

## 验收

- [x] L1～4 ExpToNext=1 Convert=1；L5 Exp 空 Convert=1
- [x] EquipEffect 累计 `GraveSpawnWeightBonus_Q4/Q5/Q6` 10/20/30/40/50
- [x] 铁铲五行保留

## 依赖

- PE-05
