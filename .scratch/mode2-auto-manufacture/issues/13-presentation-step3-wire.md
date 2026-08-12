---
title: AutoManufacture 演出 Step3 — 播完再 Advance + UM 自动开布阵
status: done
difficulty: 2
demo_scope: in-scope
selected_approach: A — PresentationFlags.AutoOpenFormationOnce
spec_refs:
  - SPEC_03 §3.15 §11 Step3 / UI-016
  - SPEC_03 §3.8 D-055
  - SPEC_04 §6
---

## 目标

演出完成后才 `TryAdvanceStage`；进 UM 自动打开 FormationEditor；0 兵跳过演出且不自动开布阵。

## 范围

- `AutoManufactureStageModule`：跑批后播表现，完成回调再 `_onComplete`
- `AutoManufacturePresentationFlags`
- `UpgradeManufactureStageController` Enter 消费旗标 → `HandleOpenFormation`
- 0 兵：Tips + 立即 Advance + 不置旗

## 不做

- 改选料算法 / 装备 UI

## 验收

- [x] 代码路径：crafted>0 播完 Arm + Advance；UM Consume → OpenFormation
- [x] 代码路径：0 兵 Tips + 立即 Complete + 不 Arm
- [ ] Unity 手验：有兵自动开布阵；返回回 UM；0 兵不自动开

## 依赖

- AM-12

## 实现摘要

- `AutoManufacturePresentationFlags`；StageModule / MetaShell / UM Module+Controller 接线
