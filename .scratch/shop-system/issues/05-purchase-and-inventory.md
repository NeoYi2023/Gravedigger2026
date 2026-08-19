---
title: Mode2 商店 — 购买扣精魂 + 入账/入仓 + slot sold 状态
status: done
difficulty: 2
demo_scope: 点击购买后精魂扣款、A 类装备入仓、B 类魔法书入槽、slot sold 持久化
spec_refs:
  - SPEC_03 §3.5 Mode2 商店系统（购买闭环：校验/扣款/入账 + slot 状态更新）
  - SPEC_04 §10 Mode2 商店系统：`ShopPurchaseService` 最小职责
  - SPEC_04 §6 持久化意图：ShopProgress PlayerPrefs key
  - 现有服务复用：ProtagonistEquipmentService（A）/ SpecialEquipSlotsService（B）
selected_approach: 复用现有装备/槽位服务，不新增“仓库实现”
---

## 目标

实现购买闭环的“可落地交互语义”：
1. 校验：SpiritEssence >= priceSpirit；slot 未 sold；category 与 itemId 映射合法。
2. 扣款：从玩家精魂中扣除 `priceSpirit`。
3. 入账/入仓（按归类）：
   - A 类（装备）：调用 `ProtagonistEquipmentService.TryAcquire(itemId)`
   - B 类（魔法书）：调用 `SpecialEquipSlotsService.TryEquip(itemId)`
4. 更新 UI/state：
   - slot 标记 sold（并清空/禁用购买按钮）
   - 写回 `ShopProgress.currentOffers` 并持久化
5. 行为边界：购买不触发刷新；仅由“刷新商品按钮”或“新关卡解锁 auto refresh once”改变 offers。

## 约束

- 若 Spirit 不足/映射不合法/slot sold：失败须 Toast/log，offers 不变化。
- 该切片不负责抽样算法（由 SS-03 提供 feeds）。

## 验收关注点

- 同一 slot 重复点击不再次扣款/入账。
- 关闭再打开商店：sold 状态保持（直到下一次新关卡解锁刷新一次）。

## 完成情况

1. `ShopPurchaseService.TryPurchase` 实现购买闭环：校验 slot/state → 校验精魂 → 扣款 → A 类 `ProtagonistEquipmentService.TryAcquire` / B 类 `SpecialEquipSlotsService.TryEquip` → 成功后持久化 `slot sold`。
2. `ShopProgressService` 新增 `TryMarkSlotSold`：将指定 slot 标记为 `IsSold=true` 并调用现有 `Persist()` 写回 PlayerPrefs。
3. 失败时（入仓失败/精魂不足/已 sold）不会修改 offers，并在入仓失败后补回精魂，保证状态一致。

