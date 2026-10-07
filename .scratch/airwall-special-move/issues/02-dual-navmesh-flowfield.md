---
title: 双烘焙 NavMesh 与双 FlowField
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.14
  - SPEC_03 §3.21
  - SPEC_03 §3.8 D-101
  - SPEC_04 §9.9b
  - SPEC_04 §9.22
  - SPEC_04 §6
---

切片 02：按方案 A 让 `SpecialMove=1` 的士兵穿过 `SupportsSpecialMove` 空气墙。依赖 [01-spec-authoring-config](01-spec-authoring-config.md)。

选定方案 A：默认网格含全部空气墙；特殊网格不含勾选墙。士兵按职业切 `agentTypeID`。推图双 FlowField。怪物始终走默认网格。

## 规则

- `NavMeshAreas.asset` 增加 `SpecialMove` 代理（复制 Humanoid）。
- 开战烤两张网格：默认含全部墙；特殊网格跳过 `SupportsSpecialMove` 墙。
- 普通士兵与怪物 `agentTypeID=0`；`SpecialMove=1` 士兵切第二类型。
- 推图：两套 `StaticBoxWalkableMask` / FlowField；`MassMoveScheduler` 按单位采样对应场。
- 搜打撤 / COC 同样拆墙收集；COC 洒兵时特殊移动士兵允许点在勾选墙内。
- 不改可破坏障碍雕刻语义。

## Acceptance

- [x] 枯骨刺客能穿过 `Coc_Lv2_01` 勾选样例墙；枯骨战士仍被挡住
- [x] 怪物不能穿过勾选墙
- [x] 未勾选的空气墙仍挡住所有单位
- [x] 推图 / 搜打撤同样按职业生效
- [x] 特殊移动士兵可洒在勾选墙内；普通士兵仍拒
