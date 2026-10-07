---
title: 普攻噪声与怪物狂暴运行时
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12
  - SPEC_03 §3.14
  - SPEC_03 §3.21
  - SPEC_03 §3.8 D-102
  - SPEC_04 §6
---

切片 02：共享 `CombatNoiseService` 在推图 / 搜打撤 / COC 的士兵普攻 `HitConfirm` 成功后施加职业噪声；达标改运行时 `AggroMode` 一次。依赖 [01-spec-tables-loader](01-spec-tables-loader.md)。技能钩子见 [03-skill-pipeline](03-skill-pipeline.md)。

## 规则

- 每个怪物实例本场累积；达阈值后切一次，之后不再叠。
- 圆心 = 攻击目标世界 XZ；敌人 = 存活怪物，不含障碍。
- 狂暴后为 `PassiveChase` / `StationaryPassive` 时另 `NotifyProvoked()`。
- `PushMapMonsterAgentView` 读 `_runtimeAggroMode`。
- 防守不接线。COC 仅职业普攻。样例全 0 时 no-op。

## Acceptance

- [x] `CombatNoiseService` 登记 / `ApplyPulse` / 一次狂暴事件
- [x] PushMap / SearchExtract / COC HitConfirm 成功后读职业表施加
- [x] Stage 订阅狂暴：`SetRuntimeAggroMode` + 被动态挑衅
- [x] 可破坏障碍不加噪声
