---
title: LRS-03 Config 加载 + LevelOperationDriver 图进度
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.9
  - SPEC_04 §6
  - SPEC_04 §9.31
depends_on:
  - LRS-02
---

## 落地摘要

`SubLevelConfigRow` + `LoadSubLevels` / 图校验；Driver：`TryEnterLevel`→RouteSelect、`TrySelectGameplayOption`、`TryAdvanceStage` 发奖解锁、空 UnlockNext 胜利；`FormationMapResolver` 改读 SubLevel。
