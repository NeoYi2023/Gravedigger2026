---
title: Scale move anim playback with effective speed
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_04 §15.5
  - SPEC_03 §3.12
---

# APR-01 — 移动动画播放速率

Defend / PushMap / SearchExtract 的士兵与怪物，在实际走/跑表现中：

`animator.speed = clamp(有效移速 / 标称速度, 0.5, 2)`

- 士兵标称 **3.5**；怪物走 **0.5**、跑 **1.0**
- 有效移速沿用现有计算，不把 Aggro / 追击倍率再乘一次
- 停步恢复 1
- 死亡、复活、远程停帧优先
- 不改位移与 HitConfirm

## 验收

- [x] 追击倍率或走/跑速度偏离标称时，脚步变快或变慢
- [x] 倍率 1 且速度等于标称时，播放速率仍为 1
- [x] 停步后速率回到 1
- [x] 远程停帧期间不被移动倍率覆盖
