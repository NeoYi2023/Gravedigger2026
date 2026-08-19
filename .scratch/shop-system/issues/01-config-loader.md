---
title: Mode2 商店 — 配置表加载与数据模型（Shop_ShopPoolConfig / Shop_ShopRefreshPriceConfig）
status: done
difficulty: 2
demo_scope: Mode2 商店开放、刷新、生成商品候选
spec_refs:
  - SPEC_04 §9.27 Shop_ShopPoolConfig：Disk/字段/PoolItemsRaw 编码规则
  - SPEC_04 §9.28 Shop_ShopRefreshPriceConfig：Disk/字段与缺行策略
  - SPEC_03 §3.5 Mode2 商店系统：解锁池筛选与 A/B 归类语义
  - SPEC_04 §14 配置表工程约定：Excel/CSV 命名与 Mode2 落点
selected_approach: reuse ConfigCsvRepository + 复用现有 CSV 加载管线
---

## 目标

1. 在 `ConfigCsvRepository` 中注册并加载两张新增商店配置表（Mode2 CSV root）。
2. 定义对应 row/data model（至少包含：ShopPoolId、RequiredMaxLevelNumber、ExtraUnlockCondition、PoolItemsRaw；以及 RefreshCount、RefreshPrice）。
3. 在加载/解析阶段完成对 `PoolItemsRaw` 的基础校验：
   - category 只能是 `A` 或 `B`
   - weight 非负，weight=0 忽略
   - itemId 与其 category 对应的目标配置表类型匹配（A→装备、B→魔法书）

## 约束

- 不做业务逻辑实现（生成/抽样/购买）——仅保证配置数据可被后续服务正确读取。
- 禁止硬编码任何表值；除解析规则外不在代码里写死“解锁/价格/权重”等数值。

## 验收

- 商店配置表可以在 Mode2 进档后完成加载并能在运行时查询到行。
- `PoolItemsRaw` 编码解析到结构化临时候选：`(itemId, category, weight)` 列表。

## 当前进度
- 已完成仓库内现有 `ConfigCsvRepository` 加载框架定位：需要在 Mode2 分支中补齐两张 `Shop_*` CSV 的加载入口与查询 API。
- 计划实现：
  - `ShopPoolConfigRow` / `ShopRefreshPriceConfigRow` 数据模型
  - `PoolItemsRaw` 解析：category=A/B、weight>=0（weight=0 忽略）、itemId 与对应表类型校验

## 已完成 / 验证
- 已新增 `ConfigCsvRepository` 的 Mode2 侧 `Shop_ShopPoolConfig.csv` / `Shop_ShopRefreshPriceConfig.csv` 加载与查询 API。
- 已在加载阶段对 `PoolItemsRaw` 做 category(A|B)、weight 非负、itemId 类型映射校验；weight=0 自动忽略入结果列表。
- `ReadLints`：新增/修改的 C# 文件无 linter errors。

## 对后续切片交接点
- SS-03 生成待售商品：直接消费 `ShopPoolConfigRow.PoolItems`（已结构化、已校验、已剔除 weight=0），无需再解析 `PoolItemsRaw`。
- SS-04/SS-05 UI & 购买：后续可通过 `ConfigCsvRepository.TryGetShopRefreshPrice` / `TryGetShopPool` 获取基础数据；slot 级 sold/empty 状态仍由后续服务写回 `ShopProgress`（SS-02 定义）。

