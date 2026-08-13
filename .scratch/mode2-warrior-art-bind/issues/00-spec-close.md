---
title: SPEC 关闭士兵 From-Art 与职业区全覆盖
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.8 D-056
  - SPEC_03 §3.8 D-057
  - SPEC_04 §15.2
  - SPEC_04 §13 FormationClassZone
  - SPEC_00 v0.80.4
selected_approach: B — 扩展 WarriorAppearancePrefabAssembler From-Art
---

## 目标

把「Art 已生产/修复但游戏 Prefab/Catalog 未绑定」与「新职业缺布阵区」写成可验收 SPEC，供后续切片编码。

## 范围

- D-056 / D-057 验收项
- SPEC_04 §15.2 士兵 From-Art 组装契约（方案 B）
- SPEC_04 §13 Ensure 覆盖全部 ClassId + 第二前/后排坐标
- Changelog v0.80.4

## 不做

- Unity C# / Prefab 实文件（WA-01 / WA-02）

## 验收

- [x] D-056/D-057 已写入 SPEC_03 §3.8 双语
- [x] §15.2 / §13 / §6 已锁方案 B 与禁止 GenerateAll
- [x] Changelog 已记

## 依赖

- 无

## 编码前

- 本片无编码
