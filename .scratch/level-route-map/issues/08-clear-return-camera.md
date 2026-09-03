---
title: LRM-08 通关返回镜头仪式
status: ready for handcheck
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.6 UI-031
  - SPEC_03 §3.8 D-086
  - SPEC_03 §3.9
  - SPEC_04 §6
  - SPEC_04 §9.31
approach: A
---

## 目标

子关卡通关返回 LevelRouteMap（地图模式）时：镜头先瞬时对准刚通关钉点 → 停顿 0.5s → 约 0.5s 平滑滚向当前「最新已解锁」前沿。首次打开 / 切 LevelId 页签仍瞬时对准前沿。

## 前置

- LRM-06 / LRM-07 地图模式可用

## 验收

- [x] SPEC_03 UI-031 / D-086 / §3.9 + SPEC_04 + Changelog v0.83.95（双语）
- [x] `LevelRouteSnapshot.JustClearedOptionId` 一次性字段
- [x] `LevelOperationDriver.TryAdvanceStage` 解锁回路线时写入，Publish 后清空
- [x] `LevelRouteSelectView` 协程：snap → hold → SmoothStep lerp
- [x] Hide / 再次 ApplySnapshot 取消进行中协程

## Unity 手验

1. Mode2 普通进有地图关卡 → 通关某一选项返回地图：先居中刚通关 Icon → 约 0.5s 停顿 → 再滚到新解锁选项
2. 首次进关 / 切 LevelId 页签：直接对准最新解锁，无停顿仪式
3. Stage 行回退布局（无地图 Prefab）：无竖滑仪式、行为不变
4. 通关过程中关面板 / 快速再通关：无协程泄漏或错位滚动

## 实现备注

- 方案 A：Snapshot 一次性 `JustClearedOptionId`；View 表现层协程；Driver 不写 Transform
- 常量：`ClearReturnHoldSeconds=0.5` / `ClearReturnMoveSeconds=0.5`（Realtime）
