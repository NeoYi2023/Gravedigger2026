---
title: PushMap — Combat 镜头双模式跟随
status: done
difficulty: 2
demo_scope: authorized
spec_refs:
  - SPEC_03 §3.14 镜头跟随 / CameraFollowMode / ResumeFollow
  - SPEC_04 §6 PM-09 PushMapCameraFollowController
approach: A
---

## 目标

Combat 镜头：默认粘随距 CurrentObjective 最近忠诚兵；拖拽入手动；底中「恢复跟随」回默认；全灭定格。

## 范围

- `PushMapCameraFollowController` 挂 `PushMapCamera`
- Auto / Manual；ResumeFollow 仅 Manual 显示
- StageController 开战接线；Prepare 关闭

## 不做

- 不跟叛变/主角；不做 Cinemachine / 硬边界 clamp；高度不变（Size 可由滚轮在 `[0.5,20]` 调整，见 v0.67.1）
- 不实现真实士兵 HP（失效=销毁/失活/叛变）

## 验收

- [x] 开战默认跟最近忠诚兵；失效后换目标
- [x] 拖拽切入手动；底中出现「恢复跟随」
- [x] 点按钮回 Auto 并隐藏按钮
- [x] 全灭定格；Prepare 无跟随；Size=2

## 依赖

- [03](03-stage-module-wire.md)（PushMapCamera）
- [04](04-objective-capture.md)（AdvanceView / CurrentObjective）

## 本会话交付（方案 A）

- SPEC_00 v0.67.0 + SPEC_03 §3.14 + SPEC_04 §6 + CONTEXT
- `PushMapCameraFollowController.cs`：Auto 粘随 / Manual 拖拽 / 全灭定格
- `PushMapStageController`：开战 Enable、Ended/Leave Disable；Runtime 底中「恢复跟随」

## 增量（v0.67.1 滚轮缩放）

- Combat 滚轮调 `orthographicSize`：默认开战 2；钳制 `[0.5, 20]`；前近后远；步进 0.5；不切换模式；恢复跟随不重置 Size
- SPEC_00 v0.67.1 + SPEC_03 §3.14 / SPEC_04 §6 同步
