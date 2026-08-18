---
title: SPEC 放大模型 VisualStyle 规则关闭
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.15 6b
  - SPEC_03 §3.8 D-066
  - SPEC_04 §6 / §9.9 / §9.24 / §15.2
  - SPEC_00 v0.82.60
  - CONTEXT VisualStyle / VisualModelScale
selected_approach: A — 独立放大通道；Visual.localScale=(k,k,k)；BodyRadius 与 AttackRange 均 ×k
---

## 目标

关闭「放大模型」规则：`VisualStyleId=Style_ScaleModel`（别名 `放大模型`）为与 AllIn1 材质共存的独立通道；`VisualIntensityAdd` 为缩放系数 k。

## 范围

- SPEC_03 §3.15 6b 拆材质通道 + 放大通道；术语 VisualStyle / VisualModelScale；D-066
- SPEC_04 §6 / §9.9 / §9.24 / §15.2：`VisualModelScale` 快照、Catalog Kind、不必建 `.mat`
- CONTEXT / Changelog v0.82.60 / spec-map
- 本目录 VS-01～VS-03 分步指令

## 不做

- Unity C# / Prefab / Excel（本片文档）

## 验收

- [x] §3.15 6b / §3.8 D-066 / §9.9 / §9.24 / §15.2 已写入双语
- [x] issues VS-01～VS-03 已发布

## 依赖

- 无

## 编码前

- 方案 **A** 已锁；难度 2；VS-01 起编码
