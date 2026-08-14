---
title: SPEC 矿灯规则与 D-060
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.8 D-060
  - SPEC_03 §3.10
  - SPEC_03 §3.16
  - SPEC_04 §9.6
  - SPEC_04 §9.17
  - SPEC_04 §9.25
  - SPEC_00 v0.82.14
selected_approach: A — Caps GraveSpawnWeightBonus map + spawn-time overlay; missing QualityId = 0 then bonus
---

## 目标

关闭矿灯规则：5 级、当前行累计 +10、缺席视为 0、编码 `GraveSpawnWeightBonus_{QualityId}`、验收 D-060。

## 验收

- [x] SPEC_03 §3.8 D-060；§3.10 有效权重 = 表 + 活 caps；§3.16 Demo 目录含 `Equip_MinerLamp`
- [x] SPEC_04 §9.6 / §9.17 / §9.25 / §6 GM
- [x] SPEC_00 Changelog v0.82.14；CONTEXT 术语

## 依赖

- PE-00～PE-04（done）
