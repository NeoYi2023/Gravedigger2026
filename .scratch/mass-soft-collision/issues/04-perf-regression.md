---
title: B+ 200v200 性能回归
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_04 §9.7 性能预算
  - SPEC_03 §3.12 方案 B+
---

## 目标

SoftCollision 并入后，移动逻辑主线程仍导向 ≤~2.5 ms/帧；超预算时按既有回退序调整，并可增「降 repulsion 邻域半径 / 加大 SoftCollision 分帧」。

## 范围

- 扩展 `MassPathingPerfStress` / `MassPathingPerfStressView`，把 `SoftCollisionService.Tick` 计入计时
- 记录并粘贴对比数字（含/不含 SoftCollision）
- 必要时执行回退：增大邻域查询 cell、降低 `maxBodiesPerFrame`、或合并进 MassMoveScheduler 分帧

## 不做

- 新玩法功能
- 更换渲染 / Animator 路径

## 验收

- [x] Editor Menu 可跑；数字可复现并写入 issue 或 SPEC 备注 — 菜单 `Gravedigger2026/Pathing/Run MassPathing 200v200 SoftCollision Compare (SC-04)`；离线 Mono 参考数字见下「落地记录」；**Unity Editor 权威数字待负责人跑菜单后粘贴**（同 MP-07 先例）
- [x] 无 Follow / 无全员 RVO 回归 — 未引入 Follow/RVO 路径；compare 两腿 `StructuralOk=True`（离线 verify-sc04 全过）

## 依赖

- [03](03-scheduler-wire.md)
- MP-07 压测入口

## 落地记录

- 选定方案：**A — Harness A/B 对比 + 既有分帧参数暴露**（难度 2 已确认）
- 事实核对：SC-03 已把 `SoftCollision.Tick(dt)` 接入 `MassMoveScheduler.Tick` 内部，既有 harness 的 Stopwatch **本就计入**；本切片增量 = 含/不含对比 + 回退旋钮 + 探针
- 改动：
  - `MassPathingPerfStress.cs` — `Run(..., resolveCollisions)` 末参（默认 true 语义不变；false=对照组，`ResolveCollisions=false` 时 Tick 仅清零不解算）；新增 `RunSoftCollisionCompare()`（on/off 两轮 + delta + 回退指引）；报告加 SoftCollision 状态行与 `MaxSoftResolved/frame` 探针；`FallbackGuidance` 扩为五项；`MassPathingPerfStressResult` 增 `SoftCollisionResolve` / `MaxSoftResolvedObserved`
  - `SoftCollisionService.cs` — 回退旋钮 ④ `QueryRadiusScale`（默认 1.0，乘 `RecommendedQueryRadius`，use 处 clamp ≥0）
  - `MassMoveScheduler.cs` — 回退旋钮 ⑤ `SoftCollisionMaxBodiesPerFrame`（默认 50，Tick 传入，clamp ≥1）
  - `MassPathingPerfStressChecks.cs` — `RunStructural` 并入：旋钮生效探针（budget=1 → resolved=1；scale 0.1 → 零 correction）+ compare 冒烟（40/side 两腿 StructuralOk + resolve 标志正确）
  - `MassPathingPerfStressMenu.cs` — 菜单 +「Run MassPathing 200v200 SoftCollision Compare (SC-04)」
  - `MassPathingPerfStressView.cs` — `_resolveCollisions` 序列化开关（SetupLiveCore 应用）；ContextMenu「Run Headless SoftCollision Compare (SC-04)」「Toggle SoftCollision Resolve (live)」；OnGUI live 行显示 softResolve 状态
- SPEC_04 §9.7 B+ 契约补 SC-04 落地澄清（中英；回退序追加 ④⑤）；SPEC_00 Changelog v0.74.6
- 脱机验证：`.scratch/mass-soft-collision/verify-sc04/`（Unity 2021.3.40f1 Mono mcs `-langversion:latest` + mono 运行）；SC-01/SC-02/SC-03 既有自检同跑无回归

### 参考数字（离线 Mono，Unity 2021.3.40f1 自带运行时；**非** Editor 权威值）

```
机型：本 Agent 机（Windows，Mono JIT）  日期：2026-08-07
200v200（400 agents，warmup=10 measure=120 dt=1/60）：
  ON : avg=0.205 ms  p95=0.246  max=0.284  withinBudget=YES  maxResolved/frame=50
  OFF: avg=0.128 ms  p95=0.148  max=0.174  (control: ResolveCollisions=false)
  Delta(ON-OFF): avg=+0.077 ms  p95=+0.098 ms  ≈ B+ 解算成本
  StructuralOk: on=True off=True（无 Follow / 无全员 RVO 回归）
```

### Unity Editor 权威数字（待负责人粘贴）

1. 菜单 `Gravedigger2026/Pathing/Run MassPathing 200v200 SoftCollision Compare (SC-04)`
2. Console 复制 `SC-04 SoftCollision ON/OFF compare` 块到此处：

```
机型 / Unity 版本：
ON  avg / p95 / max：
OFF avg / p95 / max：
Delta：
WithinBudget：
日期：
```

### 超预算回退序（SPEC_04 §9.7；本切片未触发，备查）

1. 增大 FlowField `cellSize`（向 0.5）
2. 降低 AttackSlot `N`（近战/远程常量）
3. 降低 `MassMoveScheduler.MaxRecalcPerFrame` / 槽刷新预算
4. **[B+]** 降 `SoftCollisionService.QueryRadiusScale`（如 0.75）
5. **[B+]** 降 `MassMoveScheduler.SoftCollisionMaxBodiesPerFrame`（加大分帧，接受分离滞后）
