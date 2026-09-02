---
title: SE-08 多点链、每点奖励、Leave 通关
status: done
difficulty: 2
demo_scope: out-of-scope
spec_refs:
  - SPEC_03 §3.19 搜集点链与奖励
  - SPEC_03 §3.9 通关 Reward
  - SPEC_04 §9.31 GatherPointRewards
  - SPEC_04 §9.5a ItemCatalog
approach: A
depends_on:
  - SE-07
---

## 目标

Continue → 下一 Objective；单点胜利入账 `GatherPointRewards`；Leave → 子关卡通关链（§3.9 Reward + RouteSelect）。

## 范围

- 解析 SubLevel `GatherPointCount` / `GatherPointRewards`（工作坊编码）
- 点胜利时 `ItemCatalog` 分发；Leave 时发子关卡 `Reward`（不重复已发点奖）
- `TryAdvanceStage` 或等价 clear 回调
- CombatDead 不复活；下一激活仍须进圈

## 不做

- 全灭 AbortLevel（SE-09）
- PushMap UI-017/018

## 验收

- [x] 2 点样例：Continue 切换 CurrentOrder；第二点须再进圈
- [x] 点奖励与关奖励入账可日志/仓库验证
- [x] Leave 回 UI-031 或 Victory 链

## 依赖

- SE-07

## 落地摘要（2026-09-02）

- 选定方案 A：Controller `RewardGrantService` 点奖（对齐 PushMap Capture）；Session Continue 推进 Order + 复位激活；Leave → Ended + `StageExpReward` + `TryAdvanceStage`
- `LevelStageContext.GatherPointRewards`；Module/MetaShell 接线仓库与 `HandleSearchExtractVictory`
- Changelog：SPEC_00 v0.83.80
