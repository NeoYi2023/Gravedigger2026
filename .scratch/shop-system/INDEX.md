# Mode2 商店系统（Shop System）

**状态：** SS-00～SS-06 **done**；D-075 全屏 Prefab + `GameplayType=Shop` 关卡接线 **done**。

| Issue | 状态 | 说明 |
|-------|------|------|
| SS-00 | done | SPEC_03 / SPEC_04 更新 + UI-026 / 商店开放/刷新/购买规则 |
| SS-01 | done | 商店配置表加载：`Shop_ShopPoolConfig` / `Shop_ShopRefreshPriceConfig` + `PoolItemsRaw` 解析 |
| SS-02 | done | 商店进度持久化：`ShopProgressService`、PlayerPrefs key 与重置时序 |
| SS-03 | done | 商品生成刷新：解锁池筛选 + 归类加权求和 + 类内 3 不重复抽样 |
| SS-04 | done | `ShopStageRoot` UI：玩家信息/6 槽商品/刷新按钮/购买按钮（UI 交互与状态显示） |
| SS-05 | done | 购买扣精魂与入账/入仓：A 类装备 `TryAcquire`、B 类魔法书 `TryEquip`、slot sold 状态 |
| SS-06 | done | 新关卡解锁触发：Meta 层在 Mode2 PushMap 结算后调用 shop unlock + auto refresh once |
| D-075 | done | 全屏 Prefab + `ShopStageModule` + Mode2 Stage1=`Shop`；局外 overlay 保留 |
