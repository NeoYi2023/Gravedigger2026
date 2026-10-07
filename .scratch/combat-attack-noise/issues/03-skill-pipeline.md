---
title: 技能效果钩子施加噪声
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12
  - SPEC_03 §3.8 D-102
  - SPEC_04 §9.21b
---

切片 03：`SkillEffectPipeline.DispatchEffectId` 在钩子匹配且 `AttackNoiseValue > 0` 时施加技能噪声，**不**要求 `EffectKind` 非空。依赖 [02-aa-runtime](02-aa-runtime.md)。COC 不接线技能管线。

## 规则

- 空 `TriggerHook` 视为匹配 `OnWarriorAaHitConfirm`。
- 圆心：命中类用 `HitCenterXZ`；自伤/自保类用士兵位置。
- 同一击可叠加职业普攻脉冲 + 技能脉冲。
- 不新增 EffectKind，不把噪声塞进 `EffectParams`。

## Acceptance

- [x] Pipeline `SetCombatNoise`；钩子匹配时 `ApplyPulse`
- [x] 空 EffectKind 仍可施加噪声
- [x] 空 TriggerHook 匹配普攻命中确认
