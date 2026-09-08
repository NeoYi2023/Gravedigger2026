---
title: SE-CAM-03 样例调参 + 手验清单
status: done
difficulty: 1
demo_scope: out-of-scope
spec_refs:
  - SPEC_03 §3.19 HoldFraming
  - SPEC_03 §3.8 D-087
  - SPEC_04 §9.20b SearchExtractHold* 样例值
approach: B
depends_on:
  - SE-CAM-02
---

## 目标

按手感收紧 Mode2 常量表样例值；完成守点镜头手验清单。

## 范围

- 调整 `SearchExtractHold*` 样例（Size 约 3～8、半径约 6～10、迟滞/平滑）→ Excel + Bake
- 手验清单（`SearchExtract_Lv1_01` / Demo 图）：
  - 半径内忠诚兵尽量入镜
  - 过远不至于贴脸；过近不至于整图过小
  - 战斗中无明显高频抖动/拉伸
  - 超半径追击兵允许出镜；镜头不跟着单兵飞走
  - Continue 回轨流畅

## 不做

- 新功能；改规则语义

## 验收

- [x] 几何盒测覆盖上表手验项（无 Play Mode）；清单供负责人勾选
- [x] 样例常量写入 SPEC_04 §9.20b：**与初值相同，Changelog v0.84.28 记锁定**

## 依赖

- SE-CAM-02

## 编码前

- 工作量：可单次完成
- 难度 1（负责人确认）
- 选定方案：**锁定当前初值**；补几何盒测 + 负责人 Play Mode 手验清单

## 落地摘要

`SearchExtractHold*` **未改表值**（Size `3`～`8`、半径 `8`、滞留 `0.8`、Out `0.2` / In `0.55`、采样 `0.2`、bias `0.65`）。`SearchExtractHoldFramingCorrectnessChecks` 增 60° Combat 几何盒测。SPEC_00 **v0.84.28**。

---

## 锁定样例（Mode2 `CombatConstantConfig`）

| ConstantKey | Value |
|-------------|-------|
| `SearchExtractHoldOrthoSizeMin` | `3` |
| `SearchExtractHoldOrthoSizeMax` | `8` |
| `SearchExtractHoldMaxPanRadius` | `8` |
| `SearchExtractHoldViewportPad` | `0.08` |
| `SearchExtractHoldTopHudPad` | `0.12` |
| `SearchExtractHoldInnerPad` | `0.18` |
| `SearchExtractHoldZoomInDelaySeconds` | `0.8` |
| `SearchExtractHoldSmoothTimeOut` | `0.2` |
| `SearchExtractHoldSmoothTimeIn` | `0.55` |
| `SearchExtractHoldSampleInterval` | `0.2` |
| `SearchExtractHoldObjectiveBias` | `0.65` |

盒测菜单：`Gravedigger2026/SearchExtract/Run HoldFraming Solver Checks (SE-CAM-01)`（SE-CAM-01 回归 + SE-CAM-03 几何）。

---

## Play Mode 手验清单（负责人）

前置：Unity Editor；Mode2 进档；Console 无配置加载失败；上表菜单应通过。

地图：`SearchExtract_Lv1_01`（权威）或 `SearchExtract_Demo_01`。

| # | 步骤 | 期望 | 勾选 |
|---|------|------|------|
| 1 | StartBattle → 进圈前沿轨接近当前 Objective | 仍是 `CameraFollowPath` 轨跟；**尚未** Hold 包围 | [ ] |
| 2 | 1 兵进圈激活 GatherCountdown | 镜头 SmoothDamp 切 Hold（不 Snap）；半径内忠诚兵尽量入镜 | [ ] |
| 3 | 近战挤在 Objective 附近 | Size 不低于约 3；不至于贴脸 | [ ] |
| 4 | 半径边缘仍有忠诚兵 | Size 不超过约 8；不至于整图过小 | [ ] |
| 5 | 倒计时战斗中观察 10s | 无明显高频抖动/拉伸（拉远快、拉近慢；采样 0.2s） | [ ] |
| 6 | 1 兵追出半径外 | 允许出镜；镜头 **不**跟着单兵飞走 | [ ] |
| 7 | 倒计时结束 UI-032 | 清场聚拢时镜头冻结，不突然拉近 | [ ] |
| 8 | 点「继续搜集」 | Hold 关闭，回轨跟随下一 Objective；过渡流畅 | [ ] |
| 9 | Hold 期间拖拽再点「恢复跟随」 | 回到 HoldFraming（非轨） | [ ] |
| 10 | Hold 期间滚轮改 Size | Hold **不**抢回该 Size | [ ] |

**备注：** Agent 本片完成样例锁定 + 几何盒测 + 清单文档；Play Mode 勾选由负责人在 Editor 执行。D-087 Demo 验收仍待本表勾选。
