---
title: 布阵地图职业区 Prefab + PlacementOrder 自动上阵 + 碰撞挤开
status: done
difficulty: 3
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.15 自动上阵
  - SPEC_04 §13 FormationClassZone
  - SPEC_04 §9.9b PlacementOrder
  - SPEC_03 §3.8 D-052
selected_approach: A — 区内螺旋采样（BodyRadius；短时 Instantiate 读区快照）
---

## 目标

地图 Prefab 增加 `FormationClassZone`；批结束后清空布阵，按 PlacementOrder 将本批新兵放入对应区并挤开。

## 范围

- `FormationClassZone` 组件（ClassId + 区域）
- 样例地图至少覆盖常用职业区
- Clear BattleFormation → 按 PlacementOrder 放置；BodyRadius 分离
- 放不下 → 留池不上阵

## 不做

- Mode2 UM 隐藏制造（AM-07）
- 失控/控制力闸门（Mode2 已屏蔽）

## 验收

- [x] 自动上阵后 Formation 含本批兵且位置在对应职业区内
- [x] 同区多兵无严重重叠（可接受挤开）
- [x] 旧池兵不自动再上

## 依赖

- AM-05

## 编码前

- 难度 3：方案比选（挤开算法）后编码

## 实现摘要

- 选定方案 A：`FormationZoneSpiralSearch` 区内螺旋 + `BodyRadius`；规则层只消费 `FormationClassZoneSnapshot`
- `FormationClassZone`（XZ AABB）+ `DefendAssetBuilder.EnsureFormationClassZones`；样例 `Ground_01`…`05` 已挂 11 常用职业区
- `AutoFormationDeployService`：`PlacementOrder` 升序仅部署本批 Id；无区/无空位留池
- `AutoManufactureStageModule`：Clear → `RunBatch`(flush Ids) → Collector 读下一关 BattleMap 区 → Deploy
- D-052 → 已实现；Changelog SPEC_00 v0.77.4
