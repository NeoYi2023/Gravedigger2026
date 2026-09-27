---
title: Scale attack anim playback to the attack interval
status: todo
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_04 §15.5
  - SPEC_03 §3.12
---

# APR-02 — 攻击动画播放速率（方案 A）

本片后置，不在 APR-01 同一会话实现。

每次 `PlayAttack` 采样一次 clip 长度：

`attackRate = clamp(clipLength × AttackSpeed × 攻速减速, 0.5, 2)`

- 士兵与怪物（Defend / PushMap / SearchExtract 共用 View）
- 远程停帧仍把速度打到 0，时长仍等于剩余前摇；解除时恢复 `attackRate`，不写死 1
- 制造界面预览攻击保持速率 1
- 不改 HitConfirm 与 `1/AttackSpeed` 间隔（怪物近战仍先结算再播动作）

## 验收

- [ ] 攻速高于/低于 clip 原长时，攻击动作相应变快/变慢
- [ ] 远程停帧仍停在 `RangedWindupHoldFrame`，前摇结束再按攻击倍率续播
- [ ] 制造预览攻击速率保持 1
- [ ] 伤害结算时机不变
