---
title: 从沙盘进入 COC 空地图
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.21
  - SPEC_03 §3.8 D-099
  - SPEC_04 §6
  - SPEC_04 §9.35
---

把 `SearchExtract_Lv2_01` 复制成 COC 地图，去掉搜打撤搜集点、决策和 Hold 镜头组件。从沙盘进入该地图并可以返回。本片不洒兵、不刷怪、不画迷雾。

依赖：01 的剩余次数。COC 行的 `GameplayConfigId` 要能解析到样例 `CocGameplayConfig`。

## 规则

- 样例地图 `Coc_Lv2_01`。保留可走面与空气墙。
- 摆上刷怪点、恰好一个最终 BOSS 用的刷怪点、至少一个小 BOSS 刷怪点、至少一个迷雾多边形（不少于 3 个点）、至少一个占领点。这些标记本片只摆好，不跑规则。
- 剩余次数大于 0 且未通关才能进入。进入不扣次。`GameplayState` 为 `CocCombat`。
- 返回沙盘不算单局失败，不扣次，不删士兵。胜负从 03 起才生效。
- 不走 `TryAdvanceStage`，不改推图模块。

## Acceptance

- [x] Excel 源表与 Bake 出的 CSV 含样例 `CocGameplayConfig`（保留表头第 1～2 行）
- [x] 沙盘 COC 方格进入 `Coc_Lv2_01`，不再是占位标题页
- [x] 地图上能找到刷怪点、迷雾多边形和占领点标记
- [x] 返回沙盘后次数不变
- [x] 次数为 0 时不能进入
