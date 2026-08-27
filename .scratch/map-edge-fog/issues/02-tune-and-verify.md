---
title: 手验与多图尺寸/透明度微调
status: todo
difficulty: 1
demo_scope: in-scope
spec_refs:
  - SPEC_04 §13 MapEdgeFog
  - .scratch/map-edge-fog/issues/01-ensure-map-edge-fog.md
depends_on:
  - ME-01
---

## 目标

在 Dig / PushMap 实机确认空白被遮、中心可玩区清晰；必要时按图调 `SizeMul` / Color，或抽共享 SO。

## 非范围

- 重做技术方案；改 CameraFog 显隐规则
