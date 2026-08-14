---
title: DigProtagonistCapabilities 科技+装备 Dig 效果叠加
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.16
  - SPEC_03 §3.13
  - SPEC_03 §3.8 D-059
  - SPEC_04 §9.6
  - SPEC_04 §9.17
selected_approach: A — 在 TechTree 重算路径上叠加装备 Dig EquipEffect（按键加法）；DigStageModule 读合并后 caps
---

## 目标

`DigProtagonistCapabilities` = Σ 科技 `AttributeModifiers` + Σ 仓内装备当前行 Dig 域 `EquipEffect`（按键加法）。获得/升级/进档后重算。

## 范围

- 改 `TechTreeService`（或抽出共享 `DigCapsRecalc`）合并装备效果
- 装备仓变更时触发重算并通知 Dig（若 Session 已开，刷新 caps 或等价）
- 解析 `EquipEffect` 复用科技 AttributeModifiers 风格（`DigCursorRadius` 等键）
- `EffectDomain` 不含 Dig 的行不参与 Dig 求和；Manufacture/Combat 空 apply

## 不做

- 正式装备 UI
- MagicBook 改动
- 制造/战斗 Token

## 验收

- [x] 仅科技：caps 与改前一致
- [x] 拥有 Dig 装备后 `DigCursorRadius`（或样例键）= 科技和 + 装备和
- [x] 装备升级后 caps 随等级行变化
- [x] Dig 阶段光标/伤害等读到合并后 caps

## 依赖

- PE-02

## 编码前

- 方案 **A** 已锁；可直接编码

## 完成备注

- `TechTreeService.BindEquipment` + `RecalcCapabilities` 叠加 Dig 域 `EquipEffect`；原地写回 `Capabilities` 供 Session 同引用
- MetaShell Awake：`_techTree.BindEquipment(_protagonistEquipment)`
- DigStageModule 订阅 `TechTree.Changed` → `DigStageController.RefreshCapabilities`（光标半径）
- SPEC_00 v0.82.8；未做 PE-04 GM
