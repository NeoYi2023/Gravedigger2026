---
title: Mode2 商店 — ShopProgressService 持久化与重置时序
status: done
difficulty: 2
demo_scope: 新关卡解锁触发自动刷新一次 + 刷新计数重置 + offers 持久化
spec_refs:
  - SPEC_03 §3.5 / Mode2 商店系统（开放门闩 + 自动刷新 + 价格递进重置）
  - SPEC_04 §6 持久化意图：新增 ShopProgress PlayerPrefs key（中英）
  - SPEC_04 §10 Mode2 商店系统：Service 分层与接口语义
selected_approach: PlayerPrefs per slot + CampaignMode（Mode2 only）
---

## 目标

1. 实现 `ShopProgressService`：在绑定 SaveSlot + CampaignMode（Mode2）时加载 `ShopProgress` 快照。
2. 持久化 `ShopProgress`：包含
   - `maxUnlockedLevelNumber`（最高解锁关卡进度标记）
   - `pendingOpenOnNewUnlock`（该次解锁是否尚未触发本轮开放/刷新）
   - `currentRefreshCount`
   - `currentOffers[6]`（每项：slotIndex、itemId、category、priceSpirit、isSold）
3. 在“新关卡解锁”事件到来时，执行：
   - 若进度提升：重置 `currentRefreshCount=0`、标记 pending，触发一次免费 offers 重生成

## 约束

- 不实现 offers 生成与购买扣款（由后续 issue 切片）。
- 不引入 Mode1 逻辑；Mode1 可以忽略 ShopProgress key。

## 验收关注点

- 关闭商店后再打开：offers/sold 状态保持（直到下一次进度提升触发自动刷新 once）。
- 连续多次“无新解锁”不重复触发自动刷新。
- 新解锁后：刷新计数重置，且自动刷新 once 生成新的 offers。

## 当前进度
- 将新增 `ShopProgressSaveData` / `ShopProgressService`（PlayerPrefs/JSON 持久化、默认 offers 初始化、`OnLevelCleared` 重置 refreshCount/pending、但不生成 offers）。

## 验证
- `ReadLints`：新增/修改的 C# 文件无 linter errors。
- JSON 字段命名与 SPEC_04 §6 一致（`maxUnlockedLevelNumber` / `pendingOpenOnNewUnlock` / `currentRefreshCount` / `currentOffers[6]`；每项含 `slotIndex/itemId/category(A|B)/priceSpirit/isSold`）。

## 对后续切片交接点
- SS-03 生成刷新时机：当 `ShopProgressService.PendingOpenOnNewUnlock==true` 且商店打开时，应生成新的 6 槽 offers 并把 pending 清掉；此时 `CurrentRefreshCount` 应保持为 0（auto refresh once ）。  
- SS-05 购买后：slot sold 状态与 itemId/priceSpirit 的写回仍通过同一份 `ShopProgress` JSON（SS-05 将补齐对 offers 数组的更新与 Persist）。  

