---
title: （可选）科技树画布学习与挖坟能力重算
status: todo
difficulty: 2
demo_scope: planned
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

- [ ] 学会后 DigDamage 等能力变化可在 Dig 中验证

## 依赖

- 可选；最早在 [03](03-dig-vertical.md) 之后；不阻塞主链 04/05
