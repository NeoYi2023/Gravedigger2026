---
title: 自检与回归清单
status: done
difficulty: 2
demo_scope: in-scope
approach: A
spec_refs:
  - SPEC_03 §3.8 D-083
  - SPEC_04 §15.5
completion_notes: |
  CorpseProjectileCorrectnessChecks.RunAll() 合并抛物线 + 砸击公式自检。
  Editor 菜单 Gravedigger2026/Combat/Run Corpse Projectile Correctness Checks (D-083)。
  SPEC_03 §3.8 D-083 标记完成；INDEX 全片 done。
---

## 目标

合并抛物线/砸击公式自检；Editor 菜单一键跑；更新 INDEX 全 done；D-083 标记 **完成**（实现侧）。

## 范围

1. **`CorpseProjectileCorrectnessChecks.RunAll()`**（或并入现有 Checks 菜单）：
   - 抛物线峰值/终点
   - `ShouldEnableCorpseSmash` 阈值
   - `ComputeSmashDamage` 公式
   - 命中半径 XZ 判定样例
2. **Editor 菜单：** `Gravedigger2026/Combat/Run Corpse Projectile Correctness Checks (D-083)`
3. **SPEC_03 §3.8：** D-083 状态 → **完成**（附 issues 路径）
4. **INDEX.md** 全片 done

## 不做

- 200v200 压测
- 新玩法数值调参（常量表策划改）

## 验收

- [x] 菜单跑通无失败
- [ ] PushMap + Defend 手验清单（CP-03/04）已勾
- [x] D-083 在 §3.8 标记完成

## 依赖

- [03-pushmap-vertical.md](03-pushmap-vertical.md)
- [04-defend-vertical.md](04-defend-vertical.md)
