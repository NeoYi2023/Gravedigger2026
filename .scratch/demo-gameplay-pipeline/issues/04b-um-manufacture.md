---
title: UpgradeManufacture — 制造士兵流水线
status: done
difficulty: 3
demo_scope: in-scope
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

- [x] 材料+精魂足够可制造；实例入池；属性符合公式抽检

## 依赖

- [04a](04a-um-upgrade.md)

## 编码前

难度 3：须方案比选（AskQuestion / 负责人选定）后再动手。**负责人 2026-07-25 选定方案 A。**

## 实现（SPEC v0.35.0，方案 A）

- 规则层：`Core/UpgradeManufacture/ManufactureService`（15 严格槽位、按 Id 路由、宝石类型互斥、预览、精魂闸门、种族/外观定稿、命名、扣仓与扣精魂）、`WarriorInstance`、`WarriorPoolService`、`WarriorStatMath`
- 配置层：`ConfigCsvRepository` 追加 BodyPart / Soul / Class / Race / Gem / ExtraEquipment / GemSuffixName / BodyAppearance 八表 + `StatBlock` / `StatFieldParser`
- 仓库：`WarehouseService` 支持 BodyPart 入账（AutoConvert 取躯体表）、按 Id 扣减、精魂扣减、Debug 套件入库
- 表现：`Gameplay/UpgradeManufacture/ManufacturePanelView` + `UpgradeManufactureStageController` 制造区接线
- 资源：`Editor/UpgradeManufacture/UmAssetBuilder` 重建制造区 UI 并生成 `Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab`（临时胶囊体）绑定进 Catalog
