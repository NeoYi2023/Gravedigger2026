---
title: PushMap — AggroMode 四态 AI
status: todo
difficulty: 3
demo_scope: deferred
spec_refs:
  - SPEC_03 §3.14 AggroMode
  - SPEC_04 §9.19 AggroMode / AlertRadius
---

## 目标

在 `MonsterAgentView`（或规则层）实现 ActiveChase / PassiveChase / StationaryActive / StationaryPassive；AttackMode 仍走命中方案 D。

## 范围

- AlertRadius 与 AttackRange 并列
- 仅对忠诚士兵触发主动发现（按 SPEC）

## 不做

- 技能施放
- 副本玩法

## 验收

- [ ] 四态各至少 1 只样例怪可区分行为

## 依赖

- [05](05-spawn-trap.md)

## 编码前

- 难度 3 方案比选；可与 PM-07 部分并行；须 Demo 授权
