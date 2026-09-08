---
title: SE-CAM-00 HoldFraming SPEC 关闭（方案 B）
status: done
difficulty: 2
demo_scope: out-of-scope
spec_refs:
  - SPEC_03 §3.19 倒计时战斗镜头 HoldFraming
  - SPEC_03 §3.8 D-087（P1 旁注）
  - SPEC_04 §9.20b SearchExtractHold*
  - SPEC_04 §9.32 开战镜头
  - SPEC_04 §6 SearchExtract
  - SPEC_00 v0.84.27
approach: B
---

## 目标

锁定 SearchExtract **仅点激活（GatherCountdown）期间** 的守点镜头规则：**HoldFraming**（视口包围 + 迟滞）；进圈前 / Continue 后仍走 `CameraFollowPath`。

## 范围

- SPEC_03 §3.19（双语）镜头行 + HoldFraming 表 + 术语 + D-087 / P1 旁注
- SPEC_04 §9.20b 新常量键样例值；§9.32 / §6 开战镜头段
- CONTEXT / spec-map / SPEC_00 Changelog v0.84.27
- `.scratch/search-extract-hold-camera/` INDEX + issues

## 不做

- C# / Prefab / Excel·CSV（SE-CAM-01 起）
- 改 PushMap Combat 轨跟随

## 验收

- [x] 方案 B 写入 SPEC 并双语同步
- [x] 时机 = countdown_only；超半径 = clamp_radius 已写明
- [x] issues 可独立 Agent 接手

## 依赖

- 无
