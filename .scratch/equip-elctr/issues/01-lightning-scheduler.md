---
title: DigLightningScheduler 与 ClearGraveByLightning
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.8 D-078
  - SPEC_03 §3.16
  - SPEC_04 §9.25
selected_approach: A — mirror DigExplosiveScheduler; no loot / no DigOnGraveClear
---

## 目标

规则层定时落雷、随机坟/随机点、无掉落清坟、主要手 ClassRestrict 入 WarriorPool。

## 验收

- [x] `DigLightningEffectConfig` / `DigLightningScheduler`
- [x] `DigSessionService.ClearGraveByLightning`
- [x] `GmSoldierGrantService` 注入；autoDeploy=false
