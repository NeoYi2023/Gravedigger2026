---
title: LRS-05 选项通关 Reward 发放
status: done
difficulty: 1
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.9
  - SPEC_04 §9.31
depends_on:
  - LRS-03
---

## 落地摘要

`LevelOperationDriver.BindRewardGrant` + `GrantOptionReward`（`LootDropParser` → `RewardGrantService`）；PushMap `CompleteLevelAfterBattleSettlement` 亦发奖。
