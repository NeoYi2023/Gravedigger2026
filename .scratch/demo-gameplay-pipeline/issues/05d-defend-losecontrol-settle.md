---
title: Defend — 失控开战 roll 与胜负 / 经验结算
status: done
difficulty: 2
demo_scope: in-scope
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

- [x] 胜利有经验入账；失败不入账本阶段经验且保留已有资源
- [x] 失控超额仍可开战；roll 行为可观察

## 依赖

- [05c](05c-defend-warrior-combat.md)（清场可检测）
- 建议接 [05c2](05c2-defend-ranged-projectile.md) 后再验远程清场，非硬阻塞

## 编码前

难度 2：须方案比选。**负责人 2026-07-26 选定方案 A。**

## 实现（SPEC v0.41.0，方案 A）

- 规则：`DefendSessionService` 开战锁定 Degree/Tier；`ResolveStartBattleRebelRolls`→`FinalLossChance`；清场→`VictorySettled(+100)`；护盾归零→`LevelFailure` + PermanentDeath 最小结算
- 配置：`ConfigCsvRepository` 加载 `Combat_LossOfControlConfig.csv`；`LossOfControlMath`
- 表现：`WarriorAgentView` Rebel 不受 EngageZone，就近打主角/兵/怪（对主角扣盾）
- 驱动：`DefendStageModule` 胜→`TryAdvanceStage`；败→`AbortLevelAsFailure`；宝石回仓+清布阵+移池
