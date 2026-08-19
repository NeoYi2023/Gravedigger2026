---
title: Mode2 商店 — 新关卡解锁触发开放 + 自动刷新一次 + 刷新计数重置
status: done
difficulty: 2
demo_scope: 通关推进后，商店开放并自动刷新 offers once
spec_refs:
  - SPEC_03 §3.5 Mode2 商店系统：Shop 开放门闩 + 解锁/重置时序 + auto refresh once
  - SPEC_04 §10 Mode2 商店系统：与关卡解锁集成点（ShopProgressService.OnLevelCleared + auto refresh once）
selected_approach: 存档级规则；新关卡解锁后触发 pending->refresh，UI 只读取
---

## 目标

1. 定义并接线“新关卡解锁事件”：
   - 在 Mode2 PushMap Boss 通关结算后（进入 `LevelSelectPanel` 前后），Meta 层读取当前 `LevelId` 的 numeric suffix，得到 `levelMaxNumber`。
2. 调用 `ShopProgressService.OnLevelCleared(levelMaxNumber)`：
   - 若 `levelMaxNumber` > 已记录最大值：更新 `maxUnlockedLevelNumber`、重置 `currentRefreshCount=0`、置 pending，并触发一次 offers 重生成（auto refresh once）。
   - 若无提升：不触发。
3. UI 侧行为：
   - 在 InSaveShell 中「商店」入口按钮仅与 Mode2 配套；
   - 在 offers 更新后按钮打开商店应立即展示新的 offers 与重置后的 refresh price 递进状态。

## 约束

- 新关卡解锁的 auto refresh once 不应扣款（不走 RefreshPrice）。
- 不改变其它 InSaveShell overlay（保持独立整屏实例的行为边界）。

## 验收关注点

- 关闭/重开商店：不会重复触发“新关卡解锁”那一次自动刷新。
- 仅当真的出现新的 unlock（maxUnlockedLevelNumber 提升）时自动刷新一次并重置刷新计数。

## 完成情况

1. `MetaShellController.HandlePushMapVictoryContinue` 在 Mode2 PushMap 结算后读取当前 `ActiveLevelId` 的 trailing digits，得到 `levelMaxNumber`。
2. 调用 `ShopProgressService.OnLevelCleared(levelMaxNumber)`：pending 置 true + offers 清空/重置 + 刷新计数归零（仅当 unlock 有提升）。
3. 当 `OnLevelCleared` 返回 true 时立刻调用 `ShopOfferRefreshService.TryAutoRefreshOnceIfPending`，生成 offers 一次并消化 pending（无扣 RefreshPrice）。

