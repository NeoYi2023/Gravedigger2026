---
title: UpgradeManufacture — 制造士兵流水线
status: todo
difficulty: 3
demo_scope: planned
spec_refs:
  - SPEC_03 §3.11 制造士兵 / 士兵属性构成
  - SPEC_04 §9.9 SoulConfig
  - SPEC_04 §9.9b ClassConfig
  - SPEC_04 §9.10～§9.15
  - SPEC_04 §9.12 BodyPartConfig
  - SPEC_04 §9.13 BodyAppearanceConfig
---

## 目标

槽位拖入 → 预览属性/精魂 → 制造 → 扣资源 → 生成 `WarriorInstance`（含外观 Id 与命名）。

## 范围

- 严格槽位类型；`Base(S)=Σ StatBonus`；种族/外观定稿（含保底）
- 临时 `Prefabs/Defend/Warriors/{AppearanceId}`

## 不做

- 宝石技能冲突细则、精致 UI、布阵

## 验收

- [ ] 材料+精魂足够可制造；实例入池；属性符合公式抽检

## 依赖

- [04a](04a-um-upgrade.md)

## 编码前

难度 3：须方案比选（AskQuestion / 负责人选定）后再动手。
