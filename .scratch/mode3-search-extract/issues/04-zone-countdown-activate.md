---
title: SE-04 进圈激活与搜集倒计时
status: done
difficulty: 3
demo_scope: out-of-scope
spec_refs:
  - SPEC_03 §3.19 激活与倒计时
  - SPEC_04 §9.32 GatherCountdownSeconds
approach: A
depends_on:
  - SE-03
---

## 目标

首名忠诚兵进当前 `CaptureZone` → 激活该点；规则层 `GatherCountdown` 递减；**尚不刷怪**。

## 范围

- View 上报 `CaptureZone.ContainsXZ`（对齐 PushMap Demo 扫描；Rebel 不触发）
- Session：`TryActivateGatherPoint`；重复进圈不重置
- 倒计时来源 §9.32/§9.33（工作坊定稿）
- 可选 HUD 剩余秒（若工作坊 4.1 选 A）

## 不做

- 波次刷怪（SE-06）
- 布阵重定位（SE-05）
- 单点胜利 / 无敌

## 验收

- [x] 进圈后日志/Tick 可见倒计时减少
- [x] 未进圈不启动
- [x] 已激活点重复进圈不重置计时

## 依赖

- SE-03
