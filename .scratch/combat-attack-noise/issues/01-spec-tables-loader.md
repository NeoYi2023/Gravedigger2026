---
title: 攻击噪声 SPEC、配置表与加载
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12
  - SPEC_03 §3.14
  - SPEC_03 §3.21
  - SPEC_03 §3.8 D-102
  - SPEC_04 §9.9b
  - SPEC_04 §9.19
  - SPEC_04 §9.21b
  - SPEC_04 §14.7
---

切片 01：写入规则并加列，**不接线运行时**。验收：加载全 0、非法值失败、CSV 由 Excel Bake。运行时见 [02-aa-runtime](02-aa-runtime.md) / [03-skill-pipeline](03-skill-pipeline.md)。

整包难度 2。选定方案 A：共享 `CombatNoiseService`。本片只做 SPEC + 表 + 加载。

## 规则

- `ClassConfig.AttackNoiseValue` / `NoiseRadius`：`float` ≥ 0；缺/空 → 0；&lt; 0 → 加载失败。
- `MonsterConfig.BerserkNoiseThreshold`：同上。`BerserkAggroMode`：同 `AggroMode` 四值；缺/空 = 达标也不切；非法 → 加载失败。
- `SkillEffectConfig` 同职业噪声两列；与 `EffectKind` 解耦。
- Mode1/Mode2 样例全部填 0；Mode1 Excel 同步空列。

## Acceptance

- [x] Excel 已改（第 1～2 行文档头保留）；见 SPEC_04 §14.7
- [x] CSV 由 Excel Bake（Mode1 与 Mode2）
- [x] 没有只改 CSV、不同步 Excel
- [x] Mode1/Mode2 样例噪声列全 0；`BerserkAggroMode` 空
- [x] `ClassConfigRow` / `MonsterConfigRow` / `SkillEffectConfigRow` + `ConfigCsvRepository` 加载
- [x] SPEC_03 / SPEC_04 双语 + Changelog v0.84.118 + CONTEXT + spec-map
