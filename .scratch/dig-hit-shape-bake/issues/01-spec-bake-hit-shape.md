---
title: Dig — SPEC 命中形 + Prefab 离线烘焙凸包
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.10 光标与挖掘触发 / DigObstacle
  - SPEC_04 §6 Dig 垂直切片
  - SPEC_04 §9.2 Dig Prefab 约定
---

## 目标

关闭「命中形 = 烘焙凸包」规则与 Prefab 数据管线；全部 `Grave_Q*` 写入可 Gizmo 预览的本地凸包。本片可同步接线运行时（见 02），或先只落数据。

## 范围

- 更新 SPEC_03 §3.10：触发改为「光标圆与坟 DigHitShape 凸包相交」；中英同步；Changelog + SPEC_00
- 更新 SPEC_04 §9.2 / §6：Dig Prefab 暴露 `DigHitShape`；离线烘焙；规则层禁止读 Sprite/像素
- 新增 `DigHitShape`（Gameplay.Dig）：序列化顶点 + `BoundingRadius`；选中 Gizmo
- Editor 菜单 Bake All Grave Hit Shapes；`BuildGrave` 挂组件占位
- 对 `Assets/Prefabs/Dig/Grave_Q1`…`Q10` 执行烘焙并保存 Prefab

## 不做

- 改 `DigObstacleRadius` 语义、Physics Overlap、运行时读贴图

## 验收

- [x] SPEC_03/04/00 已描述方案 B 命中语义与 Prefab 约定
- [x] 各 Grave Prefab 有 ≥3 顶点凸包；选中 Prefab 可见 Gizmo
- [x] 障碍生成行为仍用圆形 `DigObstacleRadius`

## 依赖

无

## 编码前

难度 2；**选定方案 B**。
