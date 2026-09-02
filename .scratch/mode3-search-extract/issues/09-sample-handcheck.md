---
title: SE-09 全灭失败与 Mode2 样例手验
status: done
difficulty: 2
demo_scope: out-of-scope
spec_refs:
  - SPEC_03 §3.19 失败
  - SPEC_03 §3.9 LevelFailure AbortLevel
  - SPEC_03 §3.8 D-087
approach: A
depends_on:
  - SE-08
---

## 目标

搜集中忠诚全灭 → 整关 `LevelFailure` → `AbortLevel` + LevelSelect；Mode2 样例关卡端到端手验清单。

## 范围

- Session：`RequestLevelFailure` when active gather && no loyal alive
- 不扣已有仓库；不清档
- 样例挂载 **SE-03 已完成**（Mode2 `Level_01` Stage5 分叉 `Opt_SE_Demo_01`）；本片补手验步骤 Markdown + 全灭路径

## 手验清单（Play Mode 负责人勾选）

- [ ] Mode2 进关 → 选 SearchExtract 选项 → Prepare 布阵 → 开战
- [ ] 进圈 → 倒计时 + 刷怪 + 重定位
- [ ] 守到倒计时结束 → UI-032 → Continue 第二点 / Leave 通关
- [ ] 故意全灭 → 回关卡列表，进度保留
- [ ] PushMap 同地图 Instant Capture **未**回归

### 手验步骤（建议）

1. Mode2 存档进关 `Level_01` → UI-031 末战分叉选 **SearchExtract**（`Opt_SE_Demo_01`）
2. Prepare 上阵 ≥1 → 开战；进圈激活搜集点 1
3. 观察顶栏倒计时、方向刷怪、布阵重定位；日志无 PushMap Capture
4. 守到倒计时结束 → UI-032；Continue → 进点 2 再进圈；或 Leave → 回路线/通关链
5. 重开同选项：进圈后故意让忠诚全灭 → Console 见 `[SearchExtractSession] LevelFailure` / `[SearchExtractStage] LevelFailure` → 回关卡列表；仓库已有物资不回扣；存档槽不清
6. 另开同地图 PushMap 选项：进圈仍应为 Instant Capture（回归）

## 不做

- D-087 Demo 验收项正式标 **完成**（须负责人 Play Mode 勾选）

## 验收

- [x] 全灭路径可复现（代码：搜集激活后无忠诚 → AbortLevel + LevelSelect）
- [x] `.scratch/mode3-search-extract/` 手验文档更新
- [x] 日志无 PushMap Capture 误触发（本片未改 PushMap Capture）

## 依赖

- SE-08

## 落地摘要（2026-09-02）

- 选定方案 A：`TryEvaluateLoyalWipe`（搜集已激活）→ `RequestLevelFailure` → `LevelFailureRequested` → Controller 直交 MetaShell `AbortLevelAsFailure` + `OpenLevelSelectPanel`；无 UI-017
- 死亡 / 叛变 / 倒计时归零无忠诚均触发评估；Leave 与失败互斥 `_outcomeSettled`
- Changelog：SPEC_00 v0.83.81
