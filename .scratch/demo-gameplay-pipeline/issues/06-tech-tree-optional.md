---
title: （可选）科技树画布学习与挖坟能力重算
status: done
difficulty: 2
demo_scope: in-scope
selected_approach: A
spec_refs:
  - SPEC_03 §3.13
  - SPEC_03 §3.6 UI-007 / UI-012
  - SPEC_04 §9.16 TechTreeConfig
  - SPEC_04 §9.17 TechEffectConfig
  - SPEC_04 §9.6 DigProtagonistCapabilities
---

## 目标

设置页科技树：学习扣 TechPoint；应用效果；重算挖坟能力。

## 范围

- 可拖动画布、节点、连线、学习点击（临时图标）
- 中心默认学会项生效

## 不做

- 功能系统名完整枚举 polish；学习失败文案精修可后置

## 验收

- [x] 学会后 DigDamage 等能力变化可在 Dig 中验证

## 依赖

- 可选；最早在 [03](03-dig-vertical.md) 之后；不阻塞主链 04/05

## 编码前

难度 2：须方案比选。**负责人 2026-07-26 选定方案 A。**

## 实现（SPEC v0.42.0，方案 A）

- 规则：`TechTreeService`（InitiallyUnlocked 自动学会、前置逆边、扣 TechPoint、AttributeModifiers 加法 → `DigProtagonistCapabilities`）
- 配置：`ConfigCsvRepository` 加载 `Tech_TechTreeConfig` / `Tech_TechEffectConfig`；根项效果样例 `DigDamage_25|DigCursorRadius_1.6`
- 表现：`Assets/Prefabs/Meta/TechTreeCanvasRoot.prefab`（节点 Prefab 摆位；平移/悬停/连线/三态色）；工具「设置」打开
- Dig：`DigStageModule` 注入 `TechTreeService.Capabilities`
- Editor：`Gravedigger2026/Tech/Generate TechTree Canvas Prefab`（或打开工程等 AutoGenerate）
