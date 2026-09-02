---
title: SE-00 搜打撤 SPEC 关闭（方案 A）
status: done
difficulty: 3
demo_scope: out-of-scope
spec_refs:
  - SPEC_03 §3.19 SearchExtract
  - SPEC_03 §3.8 D-087
  - SPEC_03 §3.9 GameplayType
  - SPEC_03 §3.6 UI-032
  - SPEC_04 §9.31 SubLevelConfig
  - SPEC_04 §9.32 SearchExtractGameplayConfig
  - SPEC_04 §9.33 SearchExtractWaveSpawnConfig
  - SPEC_04 §6 SearchExtract Stage 接线意图
  - SPEC_00 v0.83.74
approach: A
---

## 目标

锁定 Mode2 子关卡玩法 **SearchExtract（搜打撤）** 规则：独立 Session；有序搜集点 + 进圈倒计时 + 方向波次刷怪 + 布阵中心重定位 + UI-032 继续/离开；**不**改 PushMap 即时占领。

## 范围

- SPEC_03 §3.19（双语）+ 术语 + §3.8 D-087 + §3.9 `SearchExtract` + UI-032
- SPEC_04 §9.31 枚举 + §9.32～§9.33 表骨架（列 TBD）+ §6 接线意图
- CONTEXT / spec-map / SPEC_00 Changelog v0.83.74
- `.scratch/mode3-search-extract/` INDEX + issues

## 不做

- C# / Prefab / Excel·CSV（SE-01 起）

## 验收

- [x] 方案 A 写入 SPEC 并双语同步
- [x] 与 CampaignMode / PushMap Capture 区分已写明
- [x] issues 可独立 Agent 接手

## 依赖

- 无
