---
title: Defend — 失控开战 roll 与胜负 / 经验结算
status: todo
difficulty: 2
demo_scope: planned
spec_refs:
  - SPEC_03 §3.11 控制力与失控
  - SPEC_03 §3.12 失控叛变 / 胜负
  - SPEC_03 §3.9 阶段结算 / LevelFailure
  - SPEC_04 §9.20 LossOfControlConfig
---

## 目标

开战锁定 Degree/档次并做一次叛变 roll；清场胜利入账经验；护盾失败走 LevelFailure（不入账本阶段经验）。

## 范围

- FinalLossChance；叛变就近；叛变普攻扣盾
- 接关卡驱动：胜 → 下一阶段 / 关末；败 → 结束关卡

## 不做

- 技能二次失控 roll（Demo 不放技能）
- 失败结算 UI polish（字段可最小）

## 验收

- [ ] 胜利有经验入账；失败不入账本阶段经验且保留已有资源
- [ ] 失控超额仍可开战；roll 行为可观察

## 依赖

- [05c](05c-defend-warrior-combat.md)
