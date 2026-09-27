---
title: TFG-00 战术阵型多组与等级 SPEC 关闭
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.18 TacticalFormation
  - SPEC_03 §3.8 D-093
  - SPEC_03 UI-030
  - SPEC_03 UI-035
  - SPEC_04 §9.30 TacticalFormationConfig
  - SPEC_04 §6 布阵存档
  - SPEC_04 §9.7 运行时契约
  - SPEC_00 v0.84.65
approach: A
---

## 目标

锁定 D-093：手动建组、同阵型多组、等级向下取整后向下就近查表。产出 SPEC 与本目录 issues。本片不写玩法代码。

## 范围

- SPEC_03 §3.18（双语）、术语、§3.11 布阵条、§3.12 / §3.14 每组中心、UI-030 改目录、新增 UI-035、D-084/D-085 标注被修订、新增 D-093
- SPEC_04 §6、§9.7、§9.21 `FormationId`、§9.30 复合主键
- CONTEXT、spec-map、SPEC_00 Changelog v0.84.65

## 不做

- Excel / CSV / C# / Prefab（TFG-01 起）

## 验收

- [x] 中英规则与 D-093 一致：不再自动成组；一人一组；等级不由玩家点选
- [x] 查表规则写明：取 ≤ 计算等级的最大 `FormationLevel`
- [x] issues 可独立接手

## 依赖

- 无（D-084 框架已落地，本片只修订规则）
