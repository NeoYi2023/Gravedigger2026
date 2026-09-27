---
title: TFG-03 左缘阵型目录按钮只负责建组
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 UI-030
  - SPEC_03 §3.18 目录条
  - SPEC_03 §3.8 D-093
  - SPEC_04 §6 战术阵型目录条
approach: A
depends_on:
  - TFG-02
---

## 目标

`TacticalFormationSquadBarRoot` 从「已激活小队列表」改成「全部阵型的创建按钮」。Mode1 与 Mode2 都改。

## 需求指令

按钮数据 = 配置表去重后的 `FormationId`（TFG-01 的目录列表），不是 `CollectActiveSquads`。同阵型多个 `FormationLevel` 只出现一个按钮。没有已建组时条也显示。

点击调用 TFG-02 的 `TryCreateGroup`。失败（不足 Min）不改坐标、不选中。按钮不再承担选中；选中态留给 TFG-04 的卡牌。士兵栏高亮在卡牌落地前可以暂时保持「已上阵即高亮」，不要再用按钮按 `FormationId` 高亮（两个同名组无法区分）。

图标仍走 `Resources/UI/Formations/{IconAssetId}`，缺图空框加短名称。

## 不做

- 顶中卡牌、头顶图标、解散按钮（TFG-04）
- 战斗（TFG-05）

## 验收

- [x] 表里有几种阵型就有几个按钮，与当前是否已建组无关
- [x] 楔阵等级 1 与等级 5 只有一个楔阵按钮
- [x] 每次点击最多新建一组；已入组的士兵不会被再次收进
- [x] Mode1 与 Mode2 预制体都能点出组

## 依赖

- TFG-02
