---
title: Mode2 表扩列 + MagicBookConfig 空表 + Bake/加载
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_04 §9.9b ClassConfig
  - SPEC_04 §9.12 BodyPartConfig
  - SPEC_04 §9.24 MagicBookConfig
  - SPEC_04 §14 Bake Mode2 Tables
  - SPEC_03 §3.15
  - SPEC_03 §3.8 D-051
selected_approach: A — Mode2 填数 + Mode1 缺列容忍（MagicBook Mode1 仅空表头）
---

## 目标

Mode2 配置根落地 AutoManufacture 所需列与空魔法书表；Bake + `ConfigCsvRepository` 可加载。

## 范围

- Mode2 Excel/CSV：`BodyPartConfig` 增 `IsPrimaryHand` / `ClassRestrict` / `BodyPrimaryStat`（至少 1～2 行案例主要手）
- Mode2 `ClassConfig` 增 `AttackMode` / `PlacementOrder` / `DefaultAppearanceId`
- 新建空表 `Manufacture_MagicBookConfig`（仅表头，无效果行）
- Bake Mode2 + 运行时加载容忍缺列缺省

## 不做

- 自动制造算法 / Stage 模块
- 魔法书 UI / 具体 EffectPayload

## 验收

- [x] Mode2 CSV 含新列；Bake 无报错（Python bake Mode2 identical=25）
- [ ] Play Mode 选 Mode2 可加载新表/新列（**需 Editor 冒烟**：Console 应见 `MagicBook=0` 且 BodyPart/Class 无加载失败）
- [x] 样例主要手行 `IsPrimaryHand=1` 且 `ClassRestrict` 非空（`BP_Arm_Elf` / `BP_Arm_Undead`）

## 依赖

- AM-01

## 编码前

- 难度 2：方案比选（表仅 Mode2 vs Mode1 同步扩列）后编码
- **选定方案 A**（2026-08-11）

## 实现摘要

- Mode2 `BodyPart`/`Class` 扩列 + 样例主要手/次要手；Mode1 BodyPart/Class **未**扩列
- Mode1+Mode2 空 `Manufacture_MagicBookConfig`（RequirePath）
- `BodyPartConfigRow` / `ClassConfigRow` 新字段；新建 `MagicBookConfigRow`
- `ConfigCsvRepository` Optional 缺列缺省 + `LoadMagicBooks`
- 写表脚本：`.scratch/tools/am02_mode2_config_tables.py`
- D-051 **未**改状态（造兵循环属 AM-03）
