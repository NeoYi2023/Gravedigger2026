---
title: CI-03 SearchExtract wire + revive-alive handcheck
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.8 D-089
  - SPEC_03 §3.19
  - SPEC_04 §6
depends_on:
  - 02-hud-prefab-pushmap
approach: A
---

# 03 — SearchExtract 接线 + 手验

## 目标

将同一 `CombatIndicatorHud` 接到 SearchExtract；验证可复活怪算活着；完成 D-089 Demo 手验清单。

## 步骤

1. **SearchExtractSessionService：** 与 PushMap 同口径只读枚举 API（可抽共享接口，避免复制排序逻辑）。
2. **SearchExtractStageController：** `Combat` Show；Prepare / Ended / UI-032 决策 / UI-017 结算 Hide。
3. **手验：**
   - SE Combat 指示器出现；点清场后怪消失、中央计数更新
   - `Monster_01`（自复生）HP=0 假死期间：仍占格、中央计数仍算存活；彻底死亡后走 0.5s 灰+X
   - 换行与排序在 SE 波次刷怪后仍稳定
4. 勾选 D-089 Play Mode 验收项（负责人）。

## 验收

- [x] SE Combat 显示共享 HUD；决策/结算期间隐藏（代码接线；Play Mode 负责人勾选）
- [x] 可复活怪算活着（不进死亡格）（复用 SnapshotBuilder；Play Mode 负责人勾选）
- [x] 与 PushMap 共用 Prefab/View/SnapshotBuilder（无第二套实现）
- [ ] D-089 手验清单可勾选（Play Mode 负责人）

## 交付摘要（2026-09-07）

选定方案：**A**（难度 2）

| 路径 | 说明 |
|------|------|
| `ICombatIndicatorSessionReads` | 共享只读 Copy API |
| `SearchExtractSessionService` | 登记序 + CopyWarrior/MonsterIndicatorReads |
| `CombatIndicatorHudView.Bind` | 改绑接口（PushMap/SE 共用） |
| `SearchExtractStageController` | Combat Show；Prepare/Ended/UI-032/UI-017 Hide；Continue 后恢复 Show |
| SPEC_04 §6 / SPEC_00 **v0.84.20** | 接口名与 SE Hide 边界补记 |

## 备注

禁止修改 PushMap Capture / 复活规则本身。

选定方案：A（2026-09-07）
