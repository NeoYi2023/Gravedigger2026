---
title: Mode2 商店系统 — SPEC 规则关合 + UI-026
status: done
difficulty: 2
demo_scope: Mode2 内商店（全屏）
spec_refs:
  - SPEC_03 §3.1 术语表：ShopSystem / ShopProgress / ShopOffer / ShopPoolConfig / ShopRefreshPriceConfig / ShopCategory
  - SPEC_03 §3.5 工具面板：InSaveShell 左下入口加入「商店」并新增 Mode2 商店系统（开放/刷新/购买/重置）
  - SPEC_03 §3.6 UI 清单：UI-026 商店全屏界面
  - SPEC_04 §6 持久化意图：新增 ShopProgress PlayerPrefs key
  - SPEC_04 §9.27/§9.28 配置表：Shop_ShopPoolConfig / Shop_ShopRefreshPriceConfig 字段与编码
  - SPEC_04 §10 Mode2 商店系统：`ShopStageRoot` prefab、Service 分层、与关卡解锁集成点
selected_approach: B — 独立整屏 `ShopStageRoot` prefab 运行时实例化/销毁
---

## 目标

把 Mode2 商店系统的规则与数据结构在 SPEC 中闭合，并生成 UI-026 的最小接口语义，供后续分切实现按 issue 推进。

## 锁定

- 仅 Mode2 使用；入口在 `InSaveShell` 左下按钮堆叠中，位于「装备」之上（UI 语义：UI-026）。
- 开放/自动刷新触发：仅在“新关卡解锁（最高通过关卡号提升）”时发生。
- 6 项待售商品：A 类装备 3 项、B 类魔法书 3 项；不足留空、不补齐。
- 刷新价格：`Shop_ShopRefreshPriceConfig.RefreshCount/RefreshPrice`；每次解锁重置刷新次数并自动刷新一次。
- 购买闭环：精魂校验→扣款→归类入账/入仓→slot sold/empty 状态持久化。

## 验收

- [x] SPEC_03 完成商店开放/刷新/抽样/购买规则与术语（中英均更新）
- [x] SPEC_04 完成配置表字段、持久化键、`ShopStageRoot` 与 Service 分层（中英均更新）
- [x] UI-026 商店全屏界面在 SPEC_03 中落地描述

