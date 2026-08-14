---
title: 矿灯生成权重活叠加
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.8 D-060
  - SPEC_03 §3.10
  - SPEC_04 §9.6
selected_approach: A — DigProtagonistCapabilities.GraveSpawnWeightBonus + OverlaySpawnWeightBonuses at each TrySpawnOneGrave
---

## 目标

解析 `GraveSpawnWeightBonus_{QualityId}` 写入 caps；每次抽坟用活 caps 叠表权重；缺席插入。

## 验收

- [x] FromAttributeSums 收集前缀键
- [x] CopyCapabilities 拷贝 map
- [x] TrySpawnOneGrave 不烤死 Begin 权重

## 依赖

- PE-06
