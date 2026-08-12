---
title: AutoManufacture 演出 Step1 — Prefab + 士兵行 + 6 魔法书槽
status: done
difficulty: 2
demo_scope: in-scope
selected_approach: A — Runtime Build / AmAssetBuilder + PresentationController
spec_refs:
  - SPEC_03 §3.15 §11 Step1 / UI-016
  - SPEC_03 §3.8 D-055
  - SPEC_04 §6 / §13 AutoManufacturePresentationRoot
---

## 目标

阶段内展示 Step1：中央横滑士兵卡（150×200，「?」42 + 职业名 32）+ 上方 6 魔法书槽（120×160）。

## 范围

- `AutoManufacturePresentationRoot` Prefab（或运行时 Build）
- `AutoMfgSoldierCardView` / `AutoMfgMagicBookSlotView` / Controller 绑定
- 读本批 `WarriorId[]` + `SpecialEquipSlotsService` + ClassName / 书 DisplayName

## 不做

- Step2 动画 / Step3 接线（AM-12/13）

## 验收

- [x] 代码路径：Runtime `Build` + `AmAssetBuilder` 菜单
- [x] 卡 150×200、「?」42 / 职业名 32、书槽 120×160
- [x] 书槽读 SpecialEquipSlots；空槽空框；士兵行横滑
- [ ] Unity 手验布局

## 依赖

- AM-10

## 实现摘要

- `Gameplay/AutoManufacture/*` Views + Controller.Build
- `Editor/AutoManufacture/AmAssetBuilder.cs`
