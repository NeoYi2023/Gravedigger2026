---
title: DWH-01 SPEC + 配置（WarehouseHudStats / BaseClass / LocalizedDescription）
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.10
  - SPEC_04 §9.12
  - SPEC_04 §9.34
  - SPEC_04 §14
  - SPEC_00 Changelog v0.84.08
approach: A
---

## 目标

锁定 Dig HUD Warehouse 三行图标统计规则；`BodyPartConfig.BaseClass`；新建 `Common_LocalizedDescriptionConfig`；更新 SPEC / CONTEXT / Changelog。

## 验收

- [x] SPEC_03 §3.10 WarehouseHudStats + 术语
- [x] SPEC_04 §9.12 BaseClass、§9.34、§14 Common
- [x] Mode2 Excel+CSV BodyPart 填 BaseClass；LocalizedDescription 样例行
- [x] ConfigCsvRepository 加载 BaseClass + TryGetLocalizedText

## 备注

运行时 UI 见 DWH-02。
