---
title: Dig — DigSessionService 光标圆与命中凸包相交
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.10 光标与挖掘触发
  - SPEC_04 §6 Dig 垂直切片
---

## 目标

运行时挖坟候选判定与 SPEC 一致：圆 ∩ 凸包；保持并行 DigAction / 0.2s dwell / 品质门禁。

## 范围

- `DigGraveRuntime` 增加命中多边形与粗筛半径
- `DigStageController.HandleGraveSpawned`：从 Prefab `DigHitShape` 拷入 runtime（缺省回退圆）
- `DigSessionService.CollectEligibleGravesUnderCursor`：粗筛 + 圆–凸包相交；生成采样仍只用 `ObstacleRadius`
- 纯 C# `DigHitShapeMath`：圆心在多边形内或圆与任一边距离 ≤ 半径

## 不做

- 重烘焙管线（属 01）；像素级检测；障碍改用多边形

## 验收

- [x] 光标只擦到精灵边缘外空白时不触发；盖住可见轮廓可触发
- [x] 半径内多坟仍可并行 DigAction；忙碌锁按坟
- [x] 无 `DigHitShape` 的坟仍可挖（回退圆）

## 依赖

- [01-spec-bake-hit-shape](01-spec-bake-hit-shape.md)

## 编码前

难度 2；**选定方案 B**；依赖 01 的 Prefab 数据。
