---
title: SE-CAM-02 Stage 开/关 Hold + Controller 接线
status: done
difficulty: 2
demo_scope: out-of-scope
spec_refs:
  - SPEC_03 §3.19 HoldFraming 时机 / Manual / UI-032
  - SPEC_04 §9.32 开战镜头
  - SPEC_04 §6 SearchExtract
approach: B
depends_on:
  - SE-CAM-01
---

## 目标

把 HoldFraming 接到 SearchExtract Combat 生命周期；进圈后包围、Continue 回轨。

## 范围

- 扩展 `PushMapCameraFollowController`：`SetHoldFraming` / Clear；Hold 时 `TryGetLookAt` + Size SmoothDamp（拉远/拉近两套时间）；PushMap **不**调用
- `SearchExtractStageController`：
  - `GatherPointActivated` → 开 Hold（不 Snap；当时机位为过渡起点）
  - UI-032 决策 → 冻结最后目标
  - Continue / 未激活接近 → 关 Hold 回轨
  - Ended / 全灭 → Disable
- Manual 拖拽不变；ResumeFollow 在 Hold 窗口回到 Hold
- 滚轮改 Size 后 Hold **不**抢回（同 PushMap）

## 不做

- 样例数值精调（SE-CAM-03）
- 改 PushMap 轨语义

## 验收

- [x] 开战→进圈前仍轨跟（`EnableForCombat` 清 Hold，走 `CameraFollowPath`）
- [x] 进圈激活后改包围（`GatherPointActivated` → `SetHoldFraming`，不 Snap）
- [x] UI-032 冻结最后目标（`FreezeHoldFraming`，清场不突然拉近）
- [x] Continue 关 Hold 回轨；PushMap Stage **未**调 Hold API
- [ ] Play Mode 手感勾选 → SE-CAM-03

## 落地

- `PushMapCameraFollowController`：`SetHoldFraming` / `ClearHoldFraming` / `FreezeHoldFraming`；Hold Auto 用 Solver look-at + Size SmoothDamp（Out/In）；滚轮置 `_holdUserZoomOverride`；ResumeFollow 在 Hold 窗口 `TryGetLookAt` 回 Hold
- `SearchExtractStageController`：激活开 Hold；UI-032 冻结；Continue 关 Hold；Ended/全灭 `DisableCameraFollow`
- `CameraPresentationConstants.ResolveCombatLookAt`：斜角相机中心反解地面 look-at

## 依赖

- SE-CAM-01
