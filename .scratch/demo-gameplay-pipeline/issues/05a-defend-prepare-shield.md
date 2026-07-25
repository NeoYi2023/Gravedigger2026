---
title: Defend — Prepare / 开战 / 部署 / 护盾 / 倒计时
status: todo
difficulty: 2
demo_scope: planned
spec_refs:
  - SPEC_03 §3.12 DefendPhase / StartBattle / Shield / BattleMap
  - SPEC_04 §9.7 DefendGameplayConfig（BattleMapId → Prefabs/Maps）
  - SPEC_04 §9.8 ProtagonistMaxHP → Shield
  - SPEC_04 §13 Prefabs/Maps、Prefabs/Defend
  - SPEC_03 §3.6 UI-009
---

## 目标

进入 Defend：Prepare 可改布阵；开战部署主角与士兵；护盾初值；战斗倒计时跑起来（本片可不刷怪）。

## 范围

- 按行 `BattleMapId`（合法值 `Ground_01`…`Ground_05`，与 Dig `DigMapId` **同一池**）加载：`Assets/Prefabs/Maps/{BattleMapId}.prefab`
- EngageZone 挂在该地图 Prefab 上（本片可不驱动选敌）
- 临时 `Prefabs/Defend/BattleProtagonist`
- 无上阵士兵不可开战
- 阶段与 DigMap **实例分离**（勿复用上一阶段未销毁的 Dig 地图实例）

## 不做

- 刷怪、士兵攻击、失控、胜负结算
- 使用旧路径 `Prefabs/Defend/{BattleMapId}` 或虚构的 `BattleMap_*` 逻辑名

## 验收

- [ ] Prepare → Combat 切换正确；护盾与倒计时可见
- [ ] 地图按 BattleMapId 从 Prefabs/Maps 实例化

## 依赖

- [04c](04c-um-formation.md)
