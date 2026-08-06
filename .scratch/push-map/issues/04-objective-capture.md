---
title: PushMap — 目标点链与判定圈占领
status: todo
difficulty: 3
demo_scope: deferred
spec_refs:
  - SPEC_03 §3.14 目标点链与占领
  - SPEC_04 §9.22 CaptureSeconds
---

## 目标

全队共当前目标；CaptureZone 连续 5s 无存活怪物 → 占领 → 切换下一目标；Rebel 不阻挡占领。

## 范围

- CurrentObjective = min uncaptured ObjectiveOrder
- 忠诚士兵向当前目标推进（EngageZone 战斗可打断）
- 占领事件：标记已占领；通知停刷（接 PM-05）

## 不做

- 刷怪生成本身（PM-05）
- 占领物资入账表现 polish（可日志）

## 验收

- [ ] 1→2 目标切换可验
- [ ] 圈内有怪时计时重置；无怪 5s 占领

## 依赖

- [01](01-map-prefab-markers.md)
- [03](03-stage-module-wire.md)

## 编码前

- 难度 3 方案比选；须 Demo 授权
