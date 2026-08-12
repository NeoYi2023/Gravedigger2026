---
title: AutoManufacture 演出 Step2 — 逐兵加强动画 / Idle 揭示 / 加速
status: done
difficulty: 2
demo_scope: in-scope
selected_approach: A — Controller 协程时间轴
spec_refs:
  - SPEC_03 §3.15 §11 Step2 / UI-016
  - SPEC_03 §3.8 D-055
---

## 目标

逐兵播放书框伸缩 +「加强」；目标居中→完成后左移聚焦下一兵；每 3 兵速度 +25%；完成后「?」→ Idle。

## 范围

- PresentationController 协程
- `SampleIdleSprite` 揭示
- 速度：`rate *= 1.25^floor(completed/3)`

## 不做

- 正式魔法书效果文案（保持「加强」占位）
- Step3 Advance / 自动开布阵（AM-13）

## 验收

- [x] 代码路径：CoPlay 聚焦 / 书脉冲 / 加强 / Idle / 加速
- [ ] Unity 手验动画与加速

## 依赖

- AM-11

## 实现摘要

- `AutoManufacturePresentationController.CoPlay` / `CoFocusCard` / `CoPulseBook`
