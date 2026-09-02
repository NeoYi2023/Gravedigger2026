---
title: SE-05 布阵中心重定位与空气墙
status: done
difficulty: 3
demo_scope: out-of-scope
spec_refs:
  - SPEC_03 §3.19 布阵中心重定位
  - SPEC_03 §3.14 Demo 空气墙边界
  - SPEC_04 §9.7 MassMove GoalKind
  - SPEC_03 §3.18 战术阵型虚拟中心
approach: A
depends_on:
  - SE-04
---

## 目标

激活后与倒计时并行：全队以当前 `ObjectivePoint` 为布阵中心，按开战 `BattleFormation` 相对偏移移动；撞 `AirWall` 停最后可走点。

## 范围

- StartBattle 快照 Formation 相对偏移（相对首点或 FormationHome）
- MassMove / NavMesh：Goal 为世界偏移位；战术阵型虚拟中心跟 Objective
- 与 AttackSlot 遇敌并行（PushMap 遇敌语义）
- **不等**全员就位才允许 SE-06 刷怪（规则已在 SPEC）

## 不做

- 刷怪（SE-06）
- 修改 AirWall Bake 算法（复用 PM-08）

## 验收

- [x] 激活后士兵向 Objective 周围阵型位移动
- [x] 空气墙前停下，不穿墙
- [x] 已激活战术阵型成员跟 FormationSlot

## 依赖

- SE-04
