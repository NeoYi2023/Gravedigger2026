---
title: 外观/命名/入 WarriorPool；Mode2 无 Soul、不计费用
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.15 外观 / 最终属性 / 临时仓库→池
  - SPEC_03 §3.11 外观算法（Mode2 差分 DefaultAppearanceId）
  - SPEC_04 §9.9b DefaultAppearanceId
  - SPEC_03 §3.8 D-051
selected_approach: A — 独立 AutoManufactureService 扩展（不改 Mode1 ManufactureService.PickAppearance）
---

## 目标

AutoManufacture 兵完成外观与命名，写入 WarriorPool；实例无 SoulId、ControlPowerCost=0、AttackMode 来自 ClassConfig。

## 范围

- 种族加权 + Mode2 外观回退链（含 DefaultAppearanceId）
- `WarriorName = RaceDisplayName + ClassName`
- StaticStat / MaxHP 定稿；魔法书钩子之后定稿
- 临时仓库 flush → `WarriorPoolService.Add` + 持久化

## 不做

- 自动上阵（AM-06）
- Mode2 UM UI（AM-07）

## 验收

- [x] 池内可见新兵；`SoulId` 空；`ControlPowerCost=0`；`AttackMode` 与 ClassConfig 一致
- [x] AppearanceId 非空（至少走保底/默认）

## 依赖

- AM-03
- AM-04（钩子可先 no-op）

## 编码前

- 难度 2：方案比选后编码
- **选定方案 A**（2026-08-11）：独立 Service 扩展

## 实现摘要

- `AutoCraftDraft` 扩 AppearanceId / WarriorName / BodyLife / MaxHP / RaceAdjust / BodyLevels
- `AutoManufactureService`：钩子后 `FinalizeDraft`（`WarriorStatMath` + Mode2 `PickAppearanceMode2` + 命名）；`FlushTempToPool`；注入 `WarriorPoolService`
- `MetaShellController` 构造传 `_warriorPool`；Stage 日志更新
- Mode1 `ManufactureService.PickAppearance` 未改
- SPEC_00 v0.77.3；SPEC_03 D-051 / SPEC_04 §6 备注
