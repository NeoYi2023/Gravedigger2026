---
title: SE-07 单点胜利、无敌、停刷、清怪、UI-032
status: done
difficulty: 2
demo_scope: out-of-scope
spec_refs:
  - SPEC_03 §3.19 单点胜利
  - SPEC_03 §3.6 UI-032
  - SPEC_04 §6 CombatStatusService 无敌
approach: A
depends_on:
  - SE-06
---

## 目标

倒计时结束且 ≥1 忠诚存活 → 无敌、停刷、存活怪立即死亡、弹 UI-032「继续搜集」「离开」。

## 范围

- Session：`TryCompleteGatherPoint`；`CombatStatusService` 无敌 bucket
- 规则清场（非 Loot 驱动，除非工作坊 4.2 选 B）
- Prefab `SearchExtractDecisionPanel` 或 StageRoot 内 UI-032 按钮
- 最后一点隐藏 Continue（须已知 N 与当前 Order）

## 不做

- 点奖励入账（SE-08）
- Leave → TryAdvanceStage（SE-08）
- 全灭 LevelFailure（SE-09）

## 验收

- [x] 倒计时归零 + 有兵 → 无敌 + 怪清空 + 两按钮
- [x] 最后一点仅「离开」
- [x] Continue 解除无敌（SE-08 可接）

## 依赖

- SE-06

## 落地摘要（2026-09-02）

- `CombatStatusService.ApplyWarriorInvincibleHold` + Tick 跳过 Hold
- Session：`TryCompleteGatherPoint` / `TryContinueAfterPointSuccess` / `TryLeaveAfterPointSuccess`；清怪 tag=`PointClear`
- UI：`SearchExtractDecisionPanelView` + Editor Ensure（Prefab + Resources）；Controller 无 Prefab 时 runtime 构建
- Changelog：SPEC_00 v0.83.79
