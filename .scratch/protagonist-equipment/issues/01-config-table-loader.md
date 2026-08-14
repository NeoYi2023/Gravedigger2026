---
title: ProtagonistEquipmentConfig 表 + ConfigCsvRepository 加载
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.16
  - SPEC_03 §3.8（本片补 D-059）
  - SPEC_04 §9.25
  - SPEC_04 §14
selected_approach: A — Mode1+Mode2 同步建表；样例仅 Dig 域 1 件装备多级行
---

## 目标

落地 `Protagonist_ProtagonistEquipmentConfig` Excel/CSV（Mode1+Mode2），`ConfigCsvRepository` 可按复合主键加载；至少 1 件 Dig 装备多级样例行。

## 范围

- 新建 Excel/CSV（磁盘名见 SPEC_04 §9.25）
- 新建 `ProtagonistEquipmentConfigRow`；加载进 `ConfigCsvRepository`
- 样例建议：`Equip_DigRing` Level 1～3，`EffectDomain=Dig`，`EquipEffect` 含 `DigCursorRadius_*`；填 `ExpToNextLevel` / `ConvertExpValue`
- 本片开头在 SPEC_03 §3.8 增 **D-059**（主角装备 Dig 垂直：表加载 + 仓 + caps + GM 手验）并 Changelog

## 不做

- Service / 持久化 / Dig caps 合并 / UI / GM 按钮
- SoldierManufacture / Combat Token

## 验收

- [x] Mode1+Mode2 CSV 可 Bake；运行时加载无报错
- [x] `TryGetProtagonistEquipment(equipId, level, out row)`（或等价）可取到样例行
- [x] SPEC_03 §3.8 有 D-059；SPEC_00 Changelog 已记

## 依赖

- PE-00（done）

## 编码前

- 方案 **A** 已锁（INDEX）；可直接编码

## 完成备注

- 样例：`Equip_DigRing` L1=`DigCursorRadius_0.1` Exp50；L2=`0.2` Exp100；L3=`0.35` Exp 空（满级）；`ConvertExpValue=40`
- Bake Excel→CSV Mode1/Mode2：SAME
