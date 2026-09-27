---
title: TFG-05 战斗按组独立中心并重算等级
status: done
difficulty: 3
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.18 战斗虚拟中心
  - SPEC_03 §3.18 属性与专属技能
  - SPEC_03 §3.12 FormationSlot
  - SPEC_03 §3.14 士兵推进
  - SPEC_04 §9.7 战术阵型运行时契约
  - SPEC_03 §3.8 D-093
approach: A
depends_on:
  - TFG-01
  - TFG-02
---

## 目标

开战时每一组一个虚拟中心和一套 overlay。同 `FormationId` 的第二组不再被丢掉。成员减少后若仍达到最小人数，按剩余成员重算等级并换属性。

## 需求指令

`TacticalFormationRuntimeService` 的小队字典和 `MemberRef` 改为 `GroupInstanceId`。删掉「Duplicate FormationId / Demo max 1 instance」那条跳过。

`BuildLocks` 从 TFG-02 的组快照逐组锁定：中心、朝向、成员↔槽位、Pattern 移动参数、当时的等级行（`StatModifiers` 与该行的 `ExclusiveSkillIds` / `ExclusiveSkillEffectIds`）。无匹配等级行则该组仍锁定，属性为空并 Warning。

PushMap / Defend / SearchExtract 开战仍在关掉布阵编辑器之前拷贝这些锁。每组中心各自 `Tick`（PushMap 沿 FlowField，Defend / 搜打撤守点规则保持现有中心模式）。成员 `GoalKind=FormationSlot` 指向**自己那一组**的槽位世界点。leash 用该组中心。

`TryNotifyMemberLost`：该成员离开其所在组。剩余存活 ≥ Min → 用剩余成员重算 `Floor(平均 ClassLevel)`，换成新的等级行 overlay（派生属性重算，`RemainingHp` 钳制到新 MaxHP）。&lt; Min → 只解散这一组，Goal 回退规则与现在相同（PushMap `Objective`，Defend 个人 Home = 当前世界坐标）。不写回布阵坐标。其它组不动。

一名士兵仍然最多一组，overlay 查找继续按 `WarriorId`。

Correctness 菜单补一条：同一 `FormationId` 两组同时在场，击杀使其中一组等级下降但人数仍 ≥ Min 时只换该组加成，另一组不变。

## 不做

- 布阵卡牌与头顶图标（TFG-04）
- 职业筛选
- 改 Pattern 槽位几何

## 验收

- [x] 两个楔阵组开战都有自己的中心，成员不会被编进另一组的槽位
- [x] 两组可以使用不同的 `StatModifiers`（例如平均等级 2 对 6，分别吃到 1.15 与 1.30）
- [x] 一组死到仍 ≥ Min 且平均等级跨过表门槛时，只该组换 overlay
- [x] 一组死到 &lt; Min 时只该组解散，另一组继续 `FormationSlot`
- [x] 叛变立即离开所在组并撤掉他的加成

## 依赖

- TFG-01（等级行）
- TFG-02（组快照能交给开战锁）
