---
title: Mode2 商店 — 商品生成/刷新算法（解锁池 + 归类加权求和 + 3 不重复抽样）
status: done
difficulty: 2
demo_scope: Mode2 待售商品栏（6 项）与刷新价格递进
spec_refs:
  - SPEC_03 §3.5 Mode2 商店系统（待售商品生成算法 + 刷新按钮与价格递进 + 不足留空不补齐）
  - SPEC_03 §3.5 购买闭环（扣精魂、slot sold/empty 状态）
  - SPEC_04 §9.27 Shop_ShopPoolConfig：PoolItemsRaw 编码
  - SPEC_04 §9.28 Shop_ShopRefreshPriceConfig：RefreshCount/RefreshPrice 缺行策略
  - SPEC_04 §10 Mode2 商店系统：`ShopOfferGenerator` / `ShopRefreshPriceResolver`
selected_approach: 按 SPEC 固定算法（不改抽样语义）
---

## 目标

实现后续服务所需的核心算法：
1. 根据 `ShopProgress.maxUnlockedLevelNumber` 读取 `Shop_ShopPoolConfig`，筛选满足门槛的 `ShopPoolId`。
2. 解析所有已解锁池的 `PoolItemsRaw`：
   - 分别按 A/B 归类建立临时 `byCategoryItemIdTotalWeight`
   - 对相同 `itemId` 的权重在同一归类内求和
3. 对每个归类各随机抽取 3 个 **不同** `itemId` 生成 offers（归类 A slot0..2、归类 B slot3..5）：
   - 若不同 `itemId` 数量不足 3，则只填充已有数量，剩余 slot 保持空（不做补齐）
4. 刷新行为：
   - `currentRefreshCount` 初始为 0
   - 手动刷新：扣 `Shop_ShopRefreshPriceConfig.RefreshPrice`（对应下一次 RefreshCount）
   - 缺少对应行：刷新按钮不可用，不改变当前 offers

## 约束

- 抽样必须满足“同 `itemId` 权重先汇总、同归类内不重复抽样”的语义。
- 不要求跨 A/B 去重：A/B 是两套候选池。
- 本切片不处理购买扣款与入账（由后续 SS-05 处理）。

## 验收关注点

- `A 类装备` 和 `B 类魔法书` 每次刷新后分别最多 3 个不同道具。
- 缺少足够候选时 slot 为空但不补齐。
- 刷新按钮在缺行时不可用，且价格递进严格按 RefreshCount 表行推进。

## 当前进度
- 已落地纯算法服务：
  - `ShopOfferGenerator.GenerateOffers(...)`：按 `ShopProgress.maxUnlockedLevelNumber` 过滤解锁池、对 A/B 分别按 itemId 汇总权重、在每类内加权无重复抽样最多 3 个 itemId，并从 `ItemCatalogConfigRow.SellPrice` 填充 `priceSpirit`。
  - `ShopOfferRefreshService`：提供 auto pending-once 与 manual refresh 入口（manual 仅扣 `RefreshPrice`，不做购买扣款/入账）。

## 验证
- `ReadLints`：新增/修改的 C# 文件无 linter errors。
- 关键语义对齐：
  - 解锁池筛选：`RequiredMaxLevelNumber <= ShopProgress.maxUnlockedLevelNumber`
  - A/B 分类与 itemId 权重汇总：同 `itemId` 同类内求和
  - 抽样：每类最多 3 个不同 itemId（不足则 slot 留空、不补齐）
  - manual refresh：若下一行 `Shop_ShopRefreshPriceConfig` 缺失 → 返回 false，不改变 offers

## 对后续切片交接点
- SS-04 的 `ShopStageRoot` 在商店打开时应优先调用 `ShopOfferRefreshService.TryAutoRefreshOnceIfPending(progress, configs)`，然后再读取 `progress.CurrentOffers` 渲染 6 槽。
- SS-04 的“刷新商品”按钮点击应调用 `TryManualRefresh(progress, warehouse, configs)`：
  - 成功后从 `progress.CurrentOffers` 重新刷新 UI
  - 失败（缺行/Spirit 不足）则保持当前 offers 不变

