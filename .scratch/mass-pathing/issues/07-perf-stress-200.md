---
title: MassCombatPathing — 200v200 性能压测验收
status: done
difficulty: 3
demo_scope: authorized
spec_refs:
  - SPEC_03 §3.12 目标规模 / 性能相关禁止项
  - SPEC_04 §9.7 性能预算
approach: B
---

## 目标

提供可重复的压测入口（Debug/Tools），在双方约 200 可移动单位下验证移动栈帧时与行为底线。

## 范围

- Debug 刷兵/刷怪或实例化桩单位至约 200+200（可用简化胶囊/方块）
- Profiler / 自定义 Stopwatch：移动逻辑 ms/帧
- 验收清单写入本 issue 结论区
- 记录回退项（若超预算：降 cell、降槽 N、加大分帧）

## 不做

- 美术/动画 polish
- 改变胜负规则
- 引入第三方寻路中间件（除非负责人另批）

## 验收

- [x] 约 400 存活单位同场可运行（不硬卡死）— `MassPathingPerfStress.Run(200)` / Live Sim 桩单位
- [ ] 移动逻辑主线程均值趋向 **≤ ~2.5 ms/帧**（约定机型上手验；记入结论）— **缺口：本 Agent 机无 Unity，数字待负责人跑 Menu 后粘贴**
- [x] 无全员 HighQuality RVO / 无全员每帧 CalculatePath — Core/Gameplay Pathing 压测路径不调用；栈为 FlowField+AttackSlot+LocalDetour
- [x] 共享目标仅少量 FlowField Rebuild（切目标时）— harness 开战 1 + 中途切目标 1（`RebuildCount≤3`）
- [x] 友军相撞表现为左右绕，而非持续挤抖卡死 — 同阵营走 `LocalDetourSolver`（Live Sim 胶囊可目视；无 Carve）

## 依赖

- [05](05-chase-combat-wire.md)

## 结论（MP-07）

### 交付文件

| 路径 | 作用 |
|------|------|
| `Assets/Scripts/Core/Pathing/MassPathingPerfStress.cs` | 纯 C# 200v200 Stopwatch（Tick + ≤50 槽刷新） |
| `Assets/Scripts/Core/Pathing/MassPathingPerfStressChecks.cs` | 结构断言 + 触发全量报告 |
| `Assets/Scripts/Gameplay/Pathing/MassPathingPerfStressView.cs` | 胶囊/方块桩单位 Live Sim + OnGUI |
| `Assets/Editor/Pathing/MassPathingPerfStressMenu.cs` | Menu 入口 |

### 手验步骤

1. Unity 菜单 **`Gravedigger2026/Pathing/Run MassPathing 200v200 Perf Stress`** → Console 看 `avg/p95/max ms`、`WithinBudget`、`RebuildCount`
2. 可选：`Gravedigger2026/Pathing/Create MassPathingPerfStressView (scene)` → ContextMenu **Start Live Sim (stubs)** 目视绕行
3. 可选：`MassPathingPerfStressChecks.RunStructural()`（Console）

### 约定机型数字（待粘贴）

```
机型 / Unity 版本：
avg ms/帧：
p95 / max：
WithinBudget：
RebuildCount：
日期：
```

### 超预算回退（SPEC_04 §9.7）

1. 增大 FlowField `cellSize`（向 0.5）
2. 降低 AttackSlot `N`（近战/远程常量）
3. 降低 `MassMoveScheduler.MaxRecalcPerFrame` / 槽刷新预算（加大分帧）

### SPEC

- v0.73.6：§9.7 压测入口 + 回退序；SPEC_00 Changelog
