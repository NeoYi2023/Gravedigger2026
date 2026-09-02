---
title: TF-02 Pattern Prefab 作者组件
status: done
difficulty: 1
demo_scope: in-scope
spec_refs:
  - SPEC_04 §9.30 Pattern Prefab / TacticalFormationPattern
  - SPEC_04 §13 Prefabs/Formation/Patterns
approach: A
depends_on:
  - TF-01
---

## 目标

创建 `TacticalFormationPattern` MonoBehaviour + 样例 `FormationPattern_Wedge_01.prefab`（中心、朝向、Slot_*、LeashRadius 等）。

## 范围

- 路径 `Assets/Prefabs/Formation/Patterns/`
- 组件序列化：槽位子节点、`LeashRadius`、`SlotArriveEpsilon`、`CenterMoveSpeedMul`、`FacingTurnRate`、`KeepFormationWhileEngage`
- Editor Gizmo：中心、槽位、leash 圆（可选）
- Catalog 或 `Resources` 解析 `PrefabId` → Prefab

## 不做

- 运行时组阵逻辑（TF-03）
- 战斗移动（TF-04）

## 验收

- [x] 样例 Prefab 槽位数 ≥ 样例表 `MaxMemberCount`
- [x] 加载器可读全部移动参数字段；缺省回退 documented 常量

## 依赖

- TF-01（`PrefabId` 样例行）

## 落地摘要

选定方案 A；难度 1。`TacticalFormationPattern` + `ReadMoveParams` 缺省：Leash 3 / Epsilon 0.15 / SpeedMul 1 / TurnRate 180（<0 回退；0=锁朝向）/ KeepEngage 1。`FormationPrefabCatalog.TryGetPattern`。样例 `FormationPattern_Wedge_01` 5 槽楔阵（+Z 尖端）。菜单 `Gravedigger2026/Formation/Generate Tactical Formation Pattern Prefabs`。
