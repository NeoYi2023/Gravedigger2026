---
title: PushMap — BOSS 通关与奖励钩子
status: todo
difficulty: 2
demo_scope: deferred
spec_refs:
  - SPEC_03 §3.14 BOSS 与胜负 / 经验
  - SPEC_04 §9.22 StageExpReward / CaptureLoot / DungeonUnlockIds
---

## 目标

击杀 BossPoint 怪物 → Ended → 入账 StageExpReward → TryAdvanceStage；Shield≤0 → LevelFailure；占领可发 CaptureLoot + DungeonUnlock 存档钩子（副本玩法不做）。

## 范围

- IsBoss 行 / BossPoint 一致性校验或约定
- 经验路径对齐 Defend

## 不做

- 副本玩法正文与 UI
- 完整失败结算 UI

## 验收

- [ ] BOSS 击杀推进阶段并可观察 Exp
- [ ] 护盾归零不入账并中止关卡
- [ ] 解锁 ID 写入存档集合可日志验证

## 依赖

- [05](05-spawn-trap.md)

## 编码前

- 难度 2 方案比选；须 Demo 授权
