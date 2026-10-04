---
title: TFH-02 守槽成员不受软碰撞挤离
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.18 TacticalFormation
  - SPEC_03 §3.8 D-094
  - SPEC_04 §9.7 战术阵型运行时契约
  - SPEC_04 §9.7 SoftCollision
approach: A
depends_on:
  - TFH-01
---

## 目标

人已经被 TFH-01 留在 `FormationSlot` 上之后，同阵或友军的软碰撞不能再把他们从槽上挤开。

## 需求指令

敌我软碰撞本来就互不相推（不同 `DetourGroup`），怪物 NavMesh 避障是 `NoObstacleAvoidance`。本片要消掉的是**友军** `SoftCollisionService` 对守槽成员的 incoming 修正。

`GoalKind=FormationSlot` 的单位：本帧软碰撞加在他身上的修正为 0。他可以仍对别人产生推力，但自己不被推离槽。离开阵型、解散、改成 Objective / AttackSlot / Home 后恢复原来的修正。

不要每帧把 `Transform` 写回槽位。槽位世界点仍由 `TacticalFormationRuntimeService` 给出，移动服务继续直趋该点。

Correctness：两个重叠身体，一方为 FormationSlot、一方为 Objective；Tick 后守槽方位移修正为 0，另一方仍被推开。

## 不做

- 改怪物避障类型
- 让守槽士兵变成 NavMeshObstacle
- 死亡换槽（TFH-03）

## 验收

- [ ] 守槽成员被另一名士兵身体重叠时，脚下世界坐标不被软碰撞推离槽位
- [ ] 未入组、正在追击的士兵仍会被软碰撞推开
- [ ] 解散后原成员恢复可被推动

## 依赖

- TFH-01（成员接敌时 Goal 已经是 FormationSlot）
